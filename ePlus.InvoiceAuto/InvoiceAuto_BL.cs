using ePlus.MetaData.Server;
using System;
using System.Collections.Generic;

namespace ePlus.InvoiceAuto
{
	public class InvoiceAuto_BL : ServerComponent
	{
		private DataService_BL dataService;

		private SqlLoader<InvoiceAutoItem> loader;

		public InvoiceAuto_BL()
		{
			this.dataService = new DataService_BL();
			this.loader = new SqlLoader<InvoiceAutoItem>(this.dataService);
		}

		public List<InvoiceAutoItem> ListLot(Guid id_document)
		{
			string str = "exec USP_INVOICE_AUTO_REST null, '{0}'";
			SqlLoader<InvoiceAutoItem> sqlLoader = this.loader;
			object[] idDocument = new object[] { id_document };
			return sqlLoader.GetList(str, idDocument);
		}

		public List<InvoiceAutoItem> ListRest(long id_store)
		{
			string str = "exec USP_INVOICE_AUTO_REST {0}, null";
			SqlLoader<InvoiceAutoItem> sqlLoader = this.loader;
			object[] idStore = new object[] { id_store };
			return sqlLoader.GetList(str, idStore);
		}
	}
}