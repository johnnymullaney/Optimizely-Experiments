using System;
using System.Web;
using EPiServer.Commerce.Order;
using EPiServer.Logging;
using EPiServer.ServiceLocation;
using Foundation.Experiments.Core.Config;
using Foundation.Experiments.Core.Interfaces;
using Foundation.Experiments.Rest;
using Newtonsoft.Json;
using OptimizelySDK.Entity;

namespace Foundation.Experiments.Tracking.Service
{
    public class TrackingService : ITrackingService
    {
        private static readonly Lazy<IExperimentationFactory> ExperimentationFactory = new Lazy<IExperimentationFactory>(() => ServiceLocator.Current.GetInstance<IExperimentationFactory>());
        private readonly IUserRetriever _userRetriever;
        private readonly ExperimentationOptions _options;

        private static readonly object PadLock = new object();

        public TrackingService(IUserRetriever userRetriever, ExperimentationOptions options)
        {
            _userRetriever = userRetriever;
            _options = options;
        }

        public void TrackBasketEvent(HttpContextBase httpContext, ILineItem lineItems, string currency, string lang)
        {
            var user = _userRetriever.GetUser(httpContext);

            var eventTags = new EventTags();

            eventTags.Add(DefaultKeys.Items, JsonConvert.SerializeObject(lineItems));
            eventTags.Add(DefaultKeys.Currency, currency);
            eventTags.Add(DefaultKeys.Language, lang);
            eventTags.Add(DefaultKeys.Channel, DefaultKeys.DefaultChannel);

            Track(DefaultKeys.EventBasket, user.UserId, user.UserAttributes, eventTags);
        }

        public void TrackOrderEvent(HttpContextBase httpContext, IOrderGroup cart, string lang)
        {
            var user = _userRetriever.GetUser(httpContext);

            var shipment = cart.GetFirstShipment();

            var eventTags = new EventTags();

            eventTags.Add(DefaultKeys.Items, JsonConvert.SerializeObject(shipment.LineItems));
            eventTags.Add(DefaultKeys.OneProductInOrder, shipment.LineItems.Count == 1);
            eventTags.Add(DefaultKeys.TwoThreeProductInOrder, shipment.LineItems.Count == 2 || shipment.LineItems.Count == 3);
            eventTags.Add(DefaultKeys.FourOrMoreProductInOrder, shipment.LineItems.Count > 3);
            eventTags.Add(DefaultKeys.Currency, cart.Currency);
            eventTags.Add(DefaultKeys.Language, lang);
            eventTags.Add(DefaultKeys.Channel, DefaultKeys.DefaultChannel);

            Track(DefaultKeys.EventOrder, user.UserId, user.UserAttributes, eventTags);

            eventTags = new EventTags();

            if (cart.GetTotal() > 0)
            {
                var revenueValueInPennies = cart.GetTotal().Amount * 100;
                eventTags.Add(DefaultKeys.Revenue, Convert.ToInt32(revenueValueInPennies));
                Track(DefaultKeys.EventRevenue, user.UserId, user.UserAttributes, eventTags);
            }
        }

        public void TrackProductPageView(HttpContextBase httpContext, string lang)
        {
            var user = _userRetriever.GetUser(httpContext);

            var previousUri = GetPreviousUriFromContext(httpContext);

            var eventTags = new EventTags();

            if (!string.IsNullOrEmpty(previousUri))
            {
                eventTags.Add(DefaultKeys.ParentPageUri, previousUri);
            }

            eventTags.Add(DefaultKeys.Language, lang);
            eventTags.Add(DefaultKeys.Channel, DefaultKeys.DefaultChannel);

            Track(DefaultKeys.EventProduct, user.UserId, user.UserAttributes, eventTags);
        }

        public void TrackProductListingEvent(HttpContextBase httpContext, string listingPageName, string lang)
        {
            var user = _userRetriever.GetUser(httpContext);

            var previousUri = GetPreviousUriFromContext(httpContext);

            var eventTags = new EventTags();

            eventTags.Add(DefaultKeys.ParentPageUri, previousUri);
            eventTags.Add(DefaultKeys.CategoryName, listingPageName);
            eventTags.Add(DefaultKeys.Language, lang);

            eventTags.Add(DefaultKeys.Channel, DefaultKeys.DefaultChannel);

            if (_options.RegisterAndTrackCommerceCategoriesInOptimizely)
            {
                var isSuccessful = Track(listingPageName, user.UserId, user.UserAttributes, eventTags);
                if (!isSuccessful)
                {
                    isSuccessful = CreateEvent(listingPageName, "");
                    if (isSuccessful)
                        Track(listingPageName, user.UserId, user.UserAttributes, eventTags);
                }
            }

            Track(DefaultKeys.EventCategory, user.UserId, user.UserAttributes, eventTags);
        }

        private static bool Track(string type, string userId, UserAttributes userAttributes, EventTags eventTags)
        {
            try
            {
                if (ExperimentationFactory.Value.IsConfigured)
                {
                    ExperimentationFactory.Value.Instance?.Track(type, userId, userAttributes, eventTags);
                }

                return true;
            }
            catch (Exception e)
            {
                ServiceLocator.Current.TryGetExistingInstance(out ILogger epiErrorLogger);
                epiErrorLogger?.Log(Level.Warning, "Optimizely tracking failed", e);
            }

            return false;
        }

        private string GetPreviousUriFromContext(HttpContextBase context)
        {
            if (context.Request.UrlReferrer != null)
                return context.Request.UrlReferrer.AbsoluteUri;

            return null;
        }

        private bool CreateEvent(string key, string description)
        {
            lock (PadLock)
            {
                var client = ServiceLocator.Current.GetInstance<IExperimentationClient>();
                try
                {
                    var result = client.CreateEventIfNotExists(key, description: description);
                    return result;
                }
                catch
                {
                }
            }

            return false;
        }
    }
}