//   UpdaterRegistrator.cs
//
//  Author:
//       Allis Tauri <allista@gmail.com>
//
//  Copyright (c) 2016 Allis Tauri

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace AT_Utils
{
    #region From TweakScale (reworked)
    public abstract class UpdaterRegistrator : MonoBehaviour
    {
        static bool loadedInScene = false;

        public void Start()
        {
            if(loadedInScene)
            {
                Destroy(gameObject);
                return;
            }
            loadedInScene = true;
            OnStart();
        }
        public abstract void OnStart();

        public void Update()
        {
            loadedInScene = false;
            Destroy(gameObject);

        }
    }

    [KSPAddon(KSPAddon.Startup.Instantly, true)]
    class PartUpdatereRegister : UpdaterRegistrator
    {
        static bool IsPartUpdater(Type t)
        { return !t.IsAbstract && !t.IsGenericType && t.IsSubclassOf(typeof(PartUpdater)); }

        /// <summary>
        /// Gets all PartUpdaters defined in all loaded assemblies.
        /// </summary>
        static IEnumerable<Type> all_updaters()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try    { types = assembly.GetTypes(); }
                catch(Exception) { types = Type.EmptyTypes; }
                foreach(var type in types) 
                    if(IsPartUpdater(type))
                        yield return type;
            }
        }

        //Register all found PartUpdaters
        override public void OnStart()
        {
            foreach (var updater in all_updaters())
            {
                MethodInfo register = typeof(PartUpdater).GetMethod("RegisterUpdater");
                register = register.MakeGenericMethod(new [] { updater });
                register.Invoke(null, null);
            }
        }
    }
    #endregion
}

