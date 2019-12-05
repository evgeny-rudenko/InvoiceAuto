using ePlus.MetaData.Core;
using System;
using System.Data;
using System.Windows.Forms;

namespace ePlus.InvoiceAuto
{
	public class InvoiceAutoByInvoiceAction : IPluginGridAction
	{
		public InvoiceAutoByInvoiceAction()
		{
		}

		public void Execute(IGridController gridController, DataRow row)
		{
			using (InvoiceAutoForm invoiceAutoForm = new InvoiceAutoForm(Utils.GetGuid(row, "ID_INVOICE_GLOBAL")))
			{
				invoiceAutoForm.ShowDialog();
			}
		}

		public bool IsEnabled(IGridController gridController, DataRow row)
		{
			return Utils.GetString(row, "DOCUMENT_STATE") == "PROC";
		}
	}
}