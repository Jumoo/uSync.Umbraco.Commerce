using System;
using System.Xml.Linq;
using uSync.Core;

namespace uSync.Umbraco.Commerce.Extensions
{
    public static class XElementExtensions
    {
        /// <summary>
        ///  Return the guid value or a null if there isn't one set.
        /// </summary>
        /// <remarks>
        ///  uSync works mainly in guids that can be Guid.Empty, this
        ///  helper class lets us have nullable guids which Commerce uses
        ///  internally.
        /// </remarks>
        public static Guid? GetGuidValue(this XElement node, string name)
        {
            var value = node.Element(name).ValueOrDefault(Guid.Empty);
            if (value == Guid.Empty) return null;
            return value;
        }

        /// <summary>
        ///  gets the store Id from the xml
        /// </summary>
        /// <remarks>
        ///  We do this a lot, so this just makes the code for checking etc nicer.
        /// </remarks>
        public static Guid GetStoreId(this XElement node)
            => node.Name.LocalName.Equals("Store") ? node.GetKey() : node.Element("StoreId").ValueOrDefault(Guid.Empty);

        public static void AddStoreId(this XElement node, Guid storeId)
            => node.Add(new XElement("StoreId", storeId));
    }
}
