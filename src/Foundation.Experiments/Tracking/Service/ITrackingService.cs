using System.Web;
using EPiServer.Commerce.Order;

namespace Foundation.Experiments.Tracking.Service
{
    public interface ITrackingService
    {
        void TrackBasketEvent(HttpContextBase httpContext, ILineItem lineItems, string currency, string lang);
        void TrackOrderEvent(HttpContextBase httpContext, IOrderGroup cart, string lang);
        void TrackProductPageView(HttpContextBase httpContext, string lang);
        void TrackProductListingEvent(HttpContextBase httpContext, string listingPageName, string lang);
    }
}
