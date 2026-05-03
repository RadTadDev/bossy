using System;
using Bossy.Frontend.Parsing;
using Bossy.Schema.Registry;
using UnityEngine;

namespace Bossy
{
    public interface IBossyRegisterTypeAdapterStep
    {
        /// <summary>
        /// Adds a type adapter. If this type has a default adapter, the new one will override it.
        /// </summary>
        /// <typeparam name="TAdapter">The type of the adapter.</typeparam>
        /// <returns>The builder.</returns>
        public IBossyRegisterTypeAdapterStep WithAdapter<TAdapter>() where TAdapter : ITypeAdapter, new();
        
        /// <summary>
        /// Adds a type adapter. If this type has a default adapter, the new one will override it.
        /// </summary>
        /// <param name="adapterType"></param>
        /// <typeparam name="T">The type handled by the adapter.</typeparam>
        /// <returns>The builder.</returns>
        public IBossyRegisterTypeAdapterStep WithAdapter<T>(BaseTypeAdapter<T> adapterType);
        
        /// <summary>
        /// Completes the process of building the console.
        /// </summary>
        /// <returns>The console.</returns>
        public BossyConsole Build();
    }

    internal class BossyAdapterBuilder : IBossyRegisterTypeAdapterStep
    {
        private readonly SchemaRegistry _schemaRegistry;
        private readonly TypeAdapterRegistry _typeAdapterRegistry = new();
        
        internal BossyAdapterBuilder(SchemaRegistry schemaRegistry)
        {
            _schemaRegistry = schemaRegistry;
            
            _typeAdapterRegistry.RegisterAdapter(new BoolAdapter());
            _typeAdapterRegistry.RegisterAdapter(new ByteAdapter());
            _typeAdapterRegistry.RegisterAdapter(new SByteAdapter());
            _typeAdapterRegistry.RegisterAdapter(new ShortAdapter());
            _typeAdapterRegistry.RegisterAdapter(new UShortAdapter());
            _typeAdapterRegistry.RegisterAdapter(new IntAdapter());
            _typeAdapterRegistry.RegisterAdapter(new UIntAdapter());
            _typeAdapterRegistry.RegisterAdapter(new LongAdapter());
            _typeAdapterRegistry.RegisterAdapter(new ULongAdapter());
            _typeAdapterRegistry.RegisterAdapter(new FloatAdapter());
            _typeAdapterRegistry.RegisterAdapter(new DoubleAdapter());
            _typeAdapterRegistry.RegisterAdapter(new CharAdapter());
            _typeAdapterRegistry.RegisterAdapter(new StringAdapter());

            _typeAdapterRegistry.RegisterAdapter(new Vector2Adapter());
            _typeAdapterRegistry.RegisterAdapter(new Vector3Adapter());
            _typeAdapterRegistry.RegisterAdapter(new Vector4Adapter());
            _typeAdapterRegistry.RegisterAdapter(new Vector2IntAdapter());
            _typeAdapterRegistry.RegisterAdapter(new Vector3IntAdapter());
            _typeAdapterRegistry.RegisterAdapter(new ColorAdapter());
        
            // Enum types
            _typeAdapterRegistry.RegisterAdapter(new EnumAdapter<KeyCode>());
        }
        
        public IBossyRegisterTypeAdapterStep WithAdapter<TAdapter>() where TAdapter : ITypeAdapter, new()
        {
            var instance = Activator.CreateInstance<TAdapter>();
            // _typeAdapterRegistry.RegisterAdapter(typeof(TType), instance);
            
            return this;
        }
        
        public IBossyRegisterTypeAdapterStep WithAdapter<T>(BaseTypeAdapter<T> adapter)
        {
            _typeAdapterRegistry.RegisterAdapter(adapter);
            
            return this;
        }
        
        public BossyConsole Build()
        {
            return new BossyConsole(_schemaRegistry, _typeAdapterRegistry);
        }
    }
}