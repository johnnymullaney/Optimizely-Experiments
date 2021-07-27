using System;
using System.Web;
using EPiServer.Logging;
using EPiServer.ServiceLocation;
using Foundation.Experiments.Core.Interfaces;
using OptimizelySDK;
using OptimizelySDK.OptimizelyDecisions;

namespace Foundation.Experiments.Experimentation
{
    public class ExperimentationService : IExperimentationService
    {
        private readonly IUserRetriever _userRetriever;
        private readonly IExperimentationFactory _experimentationFactory;
        private readonly ILogger _logger;

        public ExperimentationService(IUserRetriever userRetriever, IExperimentationFactory experimentationFactory)
        {
            _userRetriever = userRetriever;
            _experimentationFactory = experimentationFactory;

            ServiceLocator.Current.TryGetExistingInstance(out ILogger epiErrorLogger);
            _logger = epiErrorLogger;
        }

        public OptimizelyDecision Decide(HttpContextBase httpContext, string key)
        {
            try
            {
                var userContext = GetOptimizelyUserContext(httpContext);

                return userContext.Decide(key);
            }
            catch (Exception e)
            {
                _logger?.Log(Level.Error, $"Error thrown on Decide of Optimzely Experiment key: {key}", e);
                return null;
            }
        }

        public string GetFeatureVariableString(HttpContextBase httpContext, string featureKey, string variableKey)
        {
            var user = _userRetriever.GetUser(httpContext);

            return _experimentationFactory.Instance.GetFeatureVariableString(featureKey, variableKey, user.UserId, user.UserAttributes);
        }

        private OptimizelyUserContext GetOptimizelyUserContext(HttpContextBase httpContext)
        {
            var user = _userRetriever.GetUser(httpContext);

            return _experimentationFactory.Instance.CreateUserContext(user.UserId, user.UserAttributes);
        }
    }
}
