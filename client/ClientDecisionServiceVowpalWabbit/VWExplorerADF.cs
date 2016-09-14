using Microsoft.Research.MultiWorldTesting.ExploreLibrary;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VW;
using VW.Serializer;

namespace Microsoft.Research.MultiWorldTesting.ClientLibrary
{
    public sealed class VWExplorerADF<TContext, TActionDependentFeature> :
        VWBaseContextMapper<VowpalWabbitThreadedPrediction<TContext, TActionDependentFeature>, VowpalWabbit<TContext, TActionDependentFeature>, TContext, ActionProbability[]>,
        IContextMapper<TContext, ActionProbability[]>, INumberOfActionsProvider<TContext>
    {
        private readonly Func<TContext, IReadOnlyCollection<TActionDependentFeature>> getContextFeaturesFunc;

        /// <summary>
        /// Constructor using a memory stream.
        /// </summary>
        /// <param name="vwModelStream">The VW model memory stream.</param>
        public VWExplorerADF(
            Func<TContext, IReadOnlyCollection<TActionDependentFeature>> getContextFeaturesFunc,
            Stream vwModelStream = null, 
            ITypeInspector typeInspector = null, 
            bool developmentMode = false,
            Predicate<VowpalWabbitArguments> modelUpdatePredicate = null)
            : base(vwModelStream, typeInspector, developmentMode, modelUpdatePredicate)
        {
            this.getContextFeaturesFunc = getContextFeaturesFunc;
        }

        protected override PolicyDecision<ActionProbability[]> MapContext(VowpalWabbit<TContext, TActionDependentFeature> vw, TContext context)
        {
            if (this.developmentMode)
            {
                Trace.TraceInformation("Example Context: {0}", VowpalWabbitMultiLine.SerializeToString(vw, context, this.getContextFeaturesFunc(context)));
            }

            var vwPredictions = vw.Predict(context, this.getContextFeaturesFunc(context));
            var ap = vwPredictions
                .Select(a =>
                    new ActionProbability
                    {
                        Action = (int)(a.Index + 1),
                        Probability = a.Probability
                    })
                    .ToArray();

            var state = new VWState { ModelId = vw.Native.ID };

            return PolicyDecision.Create(ap, state);
        }

        public int GetNumberOfActions(TContext context)
        {
            var adfs = this.getContextFeaturesFunc(context);
            return adfs == null ? 0 : adfs.Count;
        }
    }
}
