using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ePlus.InvoiceAuto
{
	public class InvoiceAutoItem
	{
		private long id_goods;

		private long id_party;

		private long id_scale_ratio;

		private string scale_ratio_name;

		private string goods_name;

		private decimal store_quantity;

		private List<decimal> quantities = new List<decimal>();

		public string Goods_name
		{
			get
			{
				return this.goods_name;
			}
			set
			{
				this.goods_name = value;
			}
		}

		public long Id_goods
		{
			get
			{
				return this.id_goods;
			}
			set
			{
				this.id_goods = value;
			}
		}

		public long Id_party
		{
			get
			{
				return this.id_party;
			}
			set
			{
				this.id_party = value;
			}
		}

		public long Id_scale_ratio
		{
			get
			{
				return this.id_scale_ratio;
			}
			set
			{
				this.id_scale_ratio = value;
			}
		}

		public decimal Left_quantity
		{
			get
			{
				decimal num = new decimal(0);
				this.quantities.ForEach((decimal d) => num += d);
				return this.store_quantity - num;
			}
		}

		public List<decimal> Quantities
		{
			get
			{
				return this.quantities;
			}
		}

		public string Scale_ratio_name
		{
			get
			{
				return this.scale_ratio_name;
			}
			set
			{
				this.scale_ratio_name = value;
			}
		}

		public decimal Store_quantity
		{
			get
			{
				return this.store_quantity;
			}
			set
			{
				this.store_quantity = value;
			}
		}

		public InvoiceAutoItem()
		{
		}
	}
}