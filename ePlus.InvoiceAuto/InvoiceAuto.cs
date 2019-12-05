using ePlus.Dictionary.BusinessObjects;
using System;
using System.Collections.Generic;

namespace ePlus.InvoiceAuto
{
	public class InvoiceAuto
	{
		private List<STORE> stores = new List<STORE>();

		private List<InvoiceAutoItem> items = new List<InvoiceAutoItem>();

		public List<InvoiceAutoItem> Items
		{
			get
			{
				return this.items;
			}
		}

		public InvoiceAuto()
		{
		}
	}
}