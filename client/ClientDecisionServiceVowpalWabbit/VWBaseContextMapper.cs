using Microsoft.Research.MultiWorldTesting.ExploreLibrary;
using System;
using System.IO;
using VW;
using VW.Serializer;

namespace Microsoft.Research.MultiWorldTesting.ClientLibrary
{
    public abstract class VWBaseContextMapper<TVowpalWabbit, TContext, TAction>
        : IUpdatable<Stream>, IDisposable, IContextMapper<TContext, TAction>
        where TVowpalWabbit : class, IDisposable
    {
        protected ITypeInspector typeInspector;
        protected VowpalWabbitThreadedPredictionBase<TVowpalWabbit> vwPool;
        protected bool developmentMode;
        protected Predicate<VowpalWabbitArguments> modelUpdatePredicate;

        /// <summary>
        /// Constructor using a memory stream.
        /// </summary>
        /// <param name="vwModelStream">The VW model memory stream.</param>
        protected VWBaseContextMapper(
            Stream vwModelStream = null,
            ITypeInspector typeInspector = null,
            bool developmentMode = false,
            Predicate<VowpalWabbitArguments> modelUpdatePredicate = null)
        {
            if (typeInspector == null)
                typeInspector = TypeInspector.Default;
            this.typeInspector = typeInspector;
            this.developmentMode = developmentMode;
            this.modelUpdatePredicate = modelUpdatePredicate;
            this.Update(vwModelStream);
        }

        public bool HasModel
        {
            get
            {
                return this.vwPool != null;
            }
        }

        /// <summary>
        /// Update VW model from stream.
        /// </summary>
        /// <param name="modelStream">The model stream to load from.</param>
        public bool Update(Stream modelStream)
        {
            bool updated = false;
            if (modelStream == null)
            {
                return updated;
            }

            var model = new VowpalWabbitModel(
                new VowpalWabbitSettings("-t")
                    {
                        ModelStream = modelStream,
                        MaxExampleCacheSize = 1024,
                        TypeInspector = this.typeInspector,
                        EnableStringExampleGeneration = this.developmentMode,
                        EnableStringFloatCompact = this.developmentMode
                    });

            if (this.modelUpdatePredicate != null && !this.modelUpdatePredicate(model.Arguments))
            {
                return updated;
            }

            var newVWPool = Activator.CreateInstance(typeof(VowpalWabbitThreadedPredictionBase<TVowpalWabbit>), new object[] { model }) as TPool;
            if (newVWPool != null)
            {
                this.vwPool = newVWPool;
                updated = true;
            }          
            
            return updated;
        }

        /// <summary>
        /// Dispose the object and clean up any resources.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose the object.
        /// </summary>
        /// <param name="disposing">Whether the object is disposing resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.vwPool != null)
                {
                    this.vwPool.Dispose();
                    this.vwPool = null;
                }
            }
        }

        public PolicyDecision<TAction> MapContext(TContext context)
        {
            if (this.vwPool == null)
                throw new InvalidOperationException("A VW model must be supplied before the call to ChooseAction.");

            using (var vw = this.vwPool.GetOrCreate())
            {
                if (vw.Value == null)
                    throw new InvalidOperationException("A VW model must be supplied before the call to ChooseAction.");

                return MapContext(vw.Value, context);    
            }
        }

        protected abstract VowpalWabbitThreadedPredictionBase<TVowpalWabbit> CreatePool(VowpalWabbitSettings settings);

        protected abstract PolicyDecision<TAction> MapContext(TVowpalWabbit vw, TContext context);
    }
}
