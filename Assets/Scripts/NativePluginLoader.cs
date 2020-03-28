using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace NPL
{
    static class DynLibInterface
    {
        const int RTLD_LAZY = 0x0001;
        const int RTLD_NOW = 0x0002;
        
#if UNITY_STANDALONE_WIN
        public const string EXT = ".dll";

        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
        static public extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("kernel32", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static public extern bool FreeLibrary(IntPtr hModule);

        [DllImport("kernel32")]
        static public extern IntPtr GetProcAddress(IntPtr hModule, string procedureName);

#elif UNITY_STANDALONE_LINUX
        public const string EXT = ".so";

        [DllImport("libdl.so")]
        public static extern IntPtr dlopen(string filename, int flags);

        [DllImport("libdl.so")]
        public static extern IntPtr dlsym(IntPtr handle, string symbol);

        [DllImport("libdl.so")]
        public static extern int dlclose(IntPtr handle);

#elif UNITY_STANDALONE_OSX     
        public const string EXT = ".dylib";
        
        [DllImport("libdl.dylib")]
        public static extern IntPtr dlopen(string filename, int flags);

        [DllImport("libdl.dylib")]
        public static extern IntPtr dlsym(IntPtr handle, string symbol);

        [DllImport("libdl.dylib")]
        public static extern int dlclose(IntPtr handle);

#endif

        public static IntPtr LoadPlugin(string filepath)
        {
#if UNITY_STANDALONE_WIN
            return LoadLibrary(filepath);
#elif UNITY_STANDALONE_LINUX
            return dlopen(filepath, RTLD_NOW);
#elif UNITY_STANDALONE_OSX
            return dlopen(filepath, RTLD_NOW);
#endif
        }

        public static bool UnloadPlugin(IntPtr handle)
        {
#if UNITY_STANDALONE_WIN
            return FreeLibrary(handle);
#elif UNITY_STANDALONE_LINUX
            return dlclose(handle) == 0;
#elif UNITY_STANDALONE_OSX
            return dlclose(handle) == 0;
#endif
        }

        public static IntPtr GetPluginProcAddress(IntPtr handle, string symbol)
        {
#if UNITY_STANDALONE_WIN
            return GetProcAddress(handle, symbol);
#elif UNITY_STANDALONE_LINUX
            return dlsym(handle, symbol);
#elif UNITY_STANDALONE_OSX
            return dlsym(handle, symbol);
#endif
        }
    }
       
    [System.Serializable]
    public class NativePluginLoader : MonoBehaviour, ISerializationCallbackReceiver
    {
        static NativePluginLoader _singleton;
        Dictionary<string, IntPtr> _loadedPlugins = new Dictionary<string, IntPtr>();
        string _path;

        static NativePluginLoader singleton {
            get {
                if (_singleton == null) {
                    var go = new GameObject("PluginLoader");
                    var pl = go.AddComponent<NativePluginLoader>();
                    Debug.Assert(_singleton == pl); // should be set by awake
                }

                return _singleton;
            }
        }

        [DllImport("stub")]
        private static extern IntPtr GetUnityInterfacesPtr();

        void Awake() {
            if (_singleton != null)
            {
                Debug.LogError(
                    string.Format("Created multiple NativePluginLoader objects. Destroying duplicate created on GameObject [{0}]",
                    this.gameObject.name));
                Destroy(this);
                return;
            }

            _singleton = this;
            DontDestroyOnLoad(this.gameObject);
            _path = Application.dataPath + "/Plugins/";
            LoadAll();
        }

        void OnDestroy() {
            UnloadAll();
            _singleton = null;
        }

        // Free all loaded libraries
        void UnloadAll()
        {
            Debug.Log("Unload All");
            
            if( NimGame.UnityPluginUnload != null)
                NimGame.UnityPluginUnload();
            else
                Debug.Log("Unload Func = null");

            foreach (var kvp in _loadedPlugins) {
                bool result = DynLibInterface.UnloadPlugin(kvp.Value);
            }
            _loadedPlugins.Clear();
        }

        // Load all plugins with 'PluginAttr'
        // Load all functions with 'PluginFunctionAttr'
        void LoadAll() {
            // TODO: Could loop over just Assembly-CSharp.dll in most cases?

            // Loop over all assemblies
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies) {
                // Loop over all types
                foreach (var type in assembly.GetTypes()) {
                    // Get custom attributes for type
                    var typeAttributes = type.GetCustomAttributes(typeof(PluginAttr), true);
                    if (typeAttributes.Length > 0)
                    {
                        Debug.Assert(typeAttributes.Length == 1); // should not be possible

                        var typeAttribute = typeAttributes[0] as PluginAttr;

                        var pluginName = typeAttribute.pluginName;
                        IntPtr pluginHandle = IntPtr.Zero;
                        if (!_loadedPlugins.TryGetValue(pluginName, out pluginHandle)) {
                            var pluginPath = _path + pluginName + DynLibInterface.EXT;
                            pluginHandle = DynLibInterface.LoadPlugin(pluginPath);
                            Debug.Log("LoadPlugin " + pluginPath);
                            if (pluginHandle == IntPtr.Zero)
                                throw new System.Exception("Failed to load plugin [" + pluginPath + "]");

                            _loadedPlugins.Add(pluginName, pluginHandle);
                        }

                        // Loop over fields in type
                        var fields = type.GetFields(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                        foreach (var field in fields) {
                            // Get custom attributes for field
                            var fieldAttributes = field.GetCustomAttributes(typeof(PluginFunctionAttr), true);
                            if (fieldAttributes.Length > 0) {
                                Debug.Assert(fieldAttributes.Length == 1); // should not be possible

                                // Get PluginFunctionAttr attribute
                                var fieldAttribute = fieldAttributes[0] as PluginFunctionAttr;
                                var functionName = fieldAttribute.functionName;

                                // Get function pointer
                                var fnPtr = DynLibInterface.GetPluginProcAddress(pluginHandle, functionName);
                                if (fnPtr == IntPtr.Zero) {
                                    Debug.LogError(string.Format("Failed to find function [{0}] in plugin [{1}].", functionName, pluginName));
                                    continue;
                                }

                                // Get delegate pointer
                                var fnDelegate = Marshal.GetDelegateForFunctionPointer(fnPtr, field.FieldType);

                                // Set static field value
                                field.SetValue(null, fnDelegate);
                            }
                        }
                    }
                }
            }   
            var interf = GetUnityInterfacesPtr();
            if( interf == IntPtr.Zero)
                Debug.Log("Fail to GetUnityInterfacesPtr");
            else
                NimGame.UnityPluginLoad( GetUnityInterfacesPtr() );         
        }


        // It is *strongly* recommended to set Editor->Preferences->Script Changes While Playing = Recompile After Finished Playing
        // Properly support reload of native assemblies requires extra work.
        // However the following code will re-fixup delegates.
        // More importantly, it prevents a dangling DLL which results in a mandatory Editor reboot
        bool _reloadAfterDeserialize = false;
        void ISerializationCallbackReceiver.OnBeforeSerialize() {
            if (_loadedPlugins.Count > 0) {
                UnloadAll();
                _reloadAfterDeserialize = true;
            }
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()  {
            if (_reloadAfterDeserialize) { 
                LoadAll();
                _reloadAfterDeserialize = false;
            }
        }
    }


    // ------------------------------------------------------------------------
    // Attribute for Plugin APIs
    // ------------------------------------------------------------------------
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class PluginAttr : System.Attribute
    {
        // Fields
        public string pluginName { get; private set; }

        // Methods
        public PluginAttr(string pluginName) {
            this.pluginName = pluginName;
        }
    }


    // ------------------------------------------------------------------------
    // Attribute for functions inside a Plugin API
    // ------------------------------------------------------------------------
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class PluginFunctionAttr : System.Attribute
    {
        // Fields
        public string functionName { get; private set; }

        // Methods
        public PluginFunctionAttr(string functionName) {
            this.functionName = functionName;
        }
    }

} // namespace NPL
