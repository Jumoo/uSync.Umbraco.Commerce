using System.Threading.Tasks;

using Umbraco.Commerce.Common.Events;

namespace uSync.Umbraco.Commerce.Handlers;

interface ISyncCommerceEventHandler<TNotification>: IAsyncEventHandlerFor<TNotification> 
    where TNotification : INotificationEvent
{
    Task HandleNotificationAsync(TNotification notification);
}
