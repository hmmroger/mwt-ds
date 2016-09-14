﻿using Microsoft.Research.MultiWorldTesting.ExploreLibrary;
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
    public sealed class VWJsonExplorer :
        VWBaseContextMapper<VowpalWabbit, string, ActionProbability[]>, 
        IContextMapper<string, ActionProbability[]>, INumberOfActionsProvider<string>
    {
        public VWJsonExplorer(Stream vwModelStream = null, bool developmentMode = false)
            : base(vwModelStream, developmentMode: developmentMode)
        {
        }

        protected override VowpalWabbitThreadedPredictionBase<VowpalWabbit> CreatePool(VowpalWabbitSettings settings)
        {
            return new VowpalWabbitThreadedPrediction(settings);
        }

        protected override PolicyDecision<ActionProbability[]> MapContext(VowpalWabbit vw, string context)
        {
            using (var vwJson = new VowpalWabbitJsonSerializer(vw))
            using (VowpalWabbitExampleCollection vwExample = vwJson.ParseAndCreate(context))
            {
                if (this.developmentMode)
                    Trace.TraceInformation("Example Context: '{0}'", vwExample.VowpalWabbitString);

                var vwPredictions = vwExample.Predict(VowpalWabbitPredictionType.ActionProbabilities);

                // VW multi-label predictions are 0-based
                var ap = vwPredictions
                    .Select(a => 
                        new ActionProbability
                        { 
                            Action = (int)(a.Action + 1),
                            Probability = a.Score
                        })
                        .ToArray();
                var state = new VWState { ModelId = vw.Native.ID };

                return PolicyDecision.Create(ap, state);
            }
        }

        public int GetNumberOfActions(string context)
        {
            return VowpalWabbitJsonSerializer.GetNumberOfActionDependentExamples(context);
        }
    }
}
