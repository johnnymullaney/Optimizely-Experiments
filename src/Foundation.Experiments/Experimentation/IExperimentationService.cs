using System.Web;
using OptimizelySDK.OptimizelyDecisions;

namespace Foundation.Experiments.Experimentation
{
    public interface IExperimentationService
    {
        OptimizelyDecision Decide(HttpContextBase httpContext, string key);
        string GetFeatureVariableString(HttpContextBase httpContext, string featureKey, string variableKey);
    }
}
