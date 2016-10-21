﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ServiceModel;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace AltSource.Utilities.VSSolution.Reflection
{
    public class AssemblyLoader
    {
        private ProjectFile _projFile;
        private Assembly _assembly;

        public AssemblyLoader(ProjectFile projFile)
        {
            _projFile = projFile;
        }

        public Assembly Load()
        {
            if (null == _assembly && !string.IsNullOrEmpty(_projFile.AssemblyName))
            {
                var projPath = Path.GetDirectoryName(_projFile.FilePath);

                var outputPath = _projFile.OutputPaths
                    .First(p => p.Trim('\\','/') == "bin" || p.ToLower().Contains("debug"));

                var extension = (_projFile.OutputType == ProjectOutputType.Exe ||
                                 _projFile.OutputType == ProjectOutputType.WinExe)
                    ? "exe"
                    : "dll";

                var assemblyPath = Path.Combine(projPath, outputPath, _projFile.AssemblyName + "." + extension);

                try
                {
                    _assembly = Assembly.LoadFrom(assemblyPath);
                }
                catch
                {

                }
            }

            return _assembly;
        }

        public IEnumerable<Type> GetServiceBeseTypes()
        {
            if (null != _assembly)
            {
                Type[] hackTypes;
                try
                {
                    hackTypes = _assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException e)
                {
                    hackTypes = e.Types;
                }

                return hackTypes
                    .Where(t => t != null)
                    .Where(t => typeof(ServiceBase).IsAssignableFrom(t));
            }

            throw new Exception("Assembly not loaded");
        }

        public IEnumerable<Type> GetTypesWith<TAttribute>(bool inherit)
                              where TAttribute : System.Attribute
        {
            if (null != _assembly)
            {
                Type[] hackTypes;
                try
                {
                    hackTypes = _assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException e)
                {
                    hackTypes = e.Types;
                }

                var typesToConsider = hackTypes
                    .Where(t => t != null)
                    .SelectMany(t => GetServiceHosts(t))
                    .Union(hackTypes.Where(t => t != null));


                var interfaces = typesToConsider
                    .SelectMany(t => t.GetInterfaces()
                        .Where(i => i.IsDefined(typeof (TAttribute), false)));

                var classes = typesToConsider
                    .Where(t => 
                                t.IsDefined(typeof (TAttribute), inherit) ||
                                interfaces.Any(i => i.IsAssignableFrom(t))
                            );

                return classes
                            .Union(interfaces);
            }

            throw new Exception("Assembly not loaded");
        }


        IEnumerable<Type> GetServiceHosts(Type type)
        {
           // Console.WriteLine($"SErviceHosts {type.FullName}");
            var hackFields = type.GetFields(BindingFlags.NonPublic |
                                                  BindingFlags.Instance);

            List<Type> fieldTypes = new List<Type>( );
            foreach (var hackField in hackFields)
            {
                try
                {
                    if (hackField.FieldType.IsGenericType)
                    {
                        //TODO: get this to work will give false positives
                        //.Where(t => t == typeof (ServiceHost))
                        fieldTypes.Add(hackField.FieldType);
                    }
                }
                catch (MissingMethodException mme)
                {
                }
                catch (ReflectionTypeLoadException rtle)
                {

                }
            }
                
            var serviceHostParms = fieldTypes
                .SelectMany(t => t.GetGenericArguments());



            return serviceHostParms;
        } 
        
    }
}
