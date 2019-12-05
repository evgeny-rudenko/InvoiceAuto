using ePlus.MetaData.Core;
using System;

namespace ePlus.InvoiceAuto
{
	[Serializable]
	public class InvoiceAutoLotItem
	{
		private long id_lot;

		private Guid id_document_item;

		[TableField]
		public Guid ID_DOCUMENT_ITEM
		{
			get
			{
				return this.id_document_item;
			}
			set
			{
				this.id_document_item = value;
			}
		}

		[TableKeyField]
		public long ID_LOT
		{
			get
			{
				return this.id_lot;
			}
			set
			{
				this.id_lot = value;
			}
		}

		public InvoiceAutoLotItem()
		{
		}
	}
}