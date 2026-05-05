using System;
using Bossy.Command;
using Bossy.Frontend.Parsing;
using Bossy.Schema.Registry;
using Bossy.Utils;
using UnityEngine;

namespace Bossy
{
    /// <summary>
    /// Assists in creating a <see cref="TypeAdapterRegistry"/>.
    /// </summary>
    public interface IBossyRegisterStep
    {
        /// <summary>
        /// Adds a type adapter. If this type has a default adapter, the new one will override it.
        /// </summary>
        /// <typeparam name="TAdapter">The type of the adapter.</typeparam>
        /// <returns>The builder.</returns>
        public IBossyRegisterStep WithAdapter<TAdapter>() where TAdapter : ITypeAdapter, new();
        
        /// <summary>
        /// Adds a type adapter. If this type has a default adapter, the new one will override it.
        /// </summary>
        /// <param name="adapterType"></param>
        /// <typeparam name="T">The type handled by the adapter.</typeparam>
        /// <returns>The builder.</returns>
        public IBossyRegisterStep WithAdapter<T>(BaseTypeAdapter<T> adapterType);

        /// <summary>
        /// Adds a binder to Bossy. This binder will be used to resolve instances of objects
        /// commands ask for via the Bind attribute.
        /// </summary>
        /// <param name="binder">The binder.</param>
        /// <returns>The builder.</returns>
        public IBossyRegisterStep WithBindings(IBossyBinder binder);
        
        /// <summary>
        /// Completes the process of building the console.
        /// </summary>
        /// <returns>The console.</returns>
        public BossyConsole Build();
    }

    /// <summary>
    /// Builds the Bossy <see cref="TypeAdapterRegistry"/>.
    /// </summary>
    internal class BossyAdapterBuilder : IBossyRegisterStep
    {
        private IBossyBinder _binder;
        private readonly SchemaRegistry _schemaRegistry;
        private readonly TypeAdapterRegistry _typeAdapterRegistry = new();
        
        internal BossyAdapterBuilder(SchemaRegistry schemaRegistry)
        {
            _schemaRegistry = schemaRegistry;
            
            // Primitives
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

            // Unity primitives
            _typeAdapterRegistry.RegisterAdapter(new Vector2Adapter());
            _typeAdapterRegistry.RegisterAdapter(new Vector3Adapter());
            _typeAdapterRegistry.RegisterAdapter(new Vector4Adapter());
            _typeAdapterRegistry.RegisterAdapter(new Vector2IntAdapter());
            _typeAdapterRegistry.RegisterAdapter(new Vector3IntAdapter());
            _typeAdapterRegistry.RegisterAdapter(new ColorAdapter());
            
            // Enum types
            _typeAdapterRegistry.RegisterAdapter(new EnumAdapter<KeyCode>());
            
            // Custom
            _typeAdapterRegistry.RegisterAdapter(new ConfirmationAdapter());
        }
        
        public IBossyRegisterStep WithAdapter<TAdapter>() where TAdapter : ITypeAdapter, new()
        {
            var instance = Activator.CreateInstance<TAdapter>();

            try
            {
                var type = instance.GetType().GetGenericArguments()[0];
                _typeAdapterRegistry.RegisterAdapter(type, instance);
            }
            catch (Exception)
            {
                Log.Error($"Cannot add type adapter {typeof(TAdapter).FullName} to bossy schema registry. Expected the base class to be BaseTypeAdapter<T> but it was not.");                
            }
            
            return this;
        }
        
        public IBossyRegisterStep WithAdapter<T>(BaseTypeAdapter<T> adapter)
        {
            _typeAdapterRegistry.RegisterAdapter(adapter);
            
            return this;
        }

        public IBossyRegisterStep WithBindings(IBossyBinder binder)
        {
            _binder = binder;

            return this;
        }

        public BossyConsole Build()
        {
            return new BossyConsole(_schemaRegistry, _typeAdapterRegistry, _binder);
        }
    }
}