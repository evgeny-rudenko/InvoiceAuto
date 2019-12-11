using ePlus.Client.Core.Store;
using ePlus.Common;
using ePlus.CommonEx;
using ePlus.CommonEx.Base;
using ePlus.CommonEx.Controls;
using ePlus.CommonEx.StockFilter.New;
using ePlus.Dictionary.BusinessObjects;
using ePlus.Dictionary.Server;
using ePlus.Interfaces;
using ePlus.Invoice.BusinessObjects;
using ePlus.Invoice.Server;
using ePlus.MetaData.Client;
using ePlus.MetaData.Core;
using ePlus.MetaData.Core.MetaGe;
using ePlus.Movement.BusinessObjects;
using ePlus.Movement.Server;
using ePlus.Server.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace ePlus.InvoiceAuto
{
	public class InvoiceAutoForm : BaseDialogForm
	{
		private ePlus.InvoiceAuto.InvoiceAuto invoiceAuto = new ePlus.InvoiceAuto.InvoiceAuto();

		private Dictionary<int, string> storeDict = new Dictionary<int, string>();

		private string filter = string.Empty;

		private IContainer components;

		private TableEditorControl grdItems;

		private Panel panel1;

		private Panel panel2;

		private Button buttonCancel;

		private Panel panel4;

		private Button delButton;

		private Button buttonStore;

		private PluginBox pluginBox1;

		private Panel panel3;

		private CheckedListBox chStore;

		private PluginBox plInvoice;

		private Label label2;

		private Label label1;

		private Button bMove;

		private Button fromStore;

		private Label label3;

		private Splitter splitter1;

		private Button buttonRefreshCols;

        /// <summary>
        /// чтобы не писать две строчки 
        /// </summary>
        /// <param name="st"></param>
        public void LogTrace (String st)
        {
            var logger = new SimpleLogger();
            logger.Trace(st);
        }
		public InvoiceAutoForm()
		{
			this.InitializeComponent();
			this.grdItems.DataGridView.CellFormatting += new DataGridViewCellFormattingEventHandler(this.OnCellFormatting);
			this.grdItems.DataGridView.EditingControlShowing += new DataGridViewEditingControlShowingEventHandler(this.OnEditorShowing);
			this.grdItems.DataGridView.CurrentCellChanged += new EventHandler(this.OnCurrentCellChanged);
		}

		public InvoiceAutoForm(Guid guidInvoice) : this()
		{
			INVOICE nVOICE = (new INVOICE_BL()).Load(guidInvoice);
			this.pluginBox1.SetId(nVOICE.ID_STORE);
			this.plInvoice.SetGuid(guidInvoice);
		}

		private void bMove_Click(object sender, EventArgs e)
		{
			int num = 0;
			this.grdItems.DataGridView.EndEdit();
			this.SyncItems();
			if (!this.ValidateDocument())
			{
				return;
			}
			if (this.invoiceAuto.Items.Count == 0 || MessageBox.Show("Создать документы перемещения?", "Подтверждение", MessageBoxButtons.YesNo) != System.Windows.Forms.DialogResult.Yes)
			{
				return;
			}
			for (int i = 0; i < this.chStore.Items.Count; i++)
			{
				STORE item = (STORE)this.chStore.Items[i];
				MOVEMENT mOVEMENT = new MOVEMENT()
				{
					MNEMOCODE = DOCUMENT_DALC.GetDocumentNumber((long)8),
					DOCUMENT_STATE = (new DocumentState((EDocumentState)((long)1))).Mnemocode,
					ID_STORE_FROM = this.pluginBox1.Id,
					ID_STORE_TO = item.ID_STORE,
					DATE = DateTime.Now
				};
				mOVEMENT.Items.Clear();
				foreach (InvoiceAutoItem invoiceAutoItem in this.invoiceAuto.Items)
				{
					if (invoiceAutoItem.Left_quantity < new decimal(0) || i >= invoiceAutoItem.Quantities.Count || invoiceAutoItem.Quantities[i] == new decimal(0))
					{
						continue;
					}
					MOVEMENT_ITEM mOVEMENTITEM = new MOVEMENT_ITEM()
					{
						ID_LOT_FROM = invoiceAutoItem.Id_party,
						QUANTITY = invoiceAutoItem.Quantities[i] 
					};

                   // mOVEMENTITEM.QUANTITY = Math.Floor(mOVEMENTITEM.QUANTITY); //2019 почему то попадают десятичные значения в количество
                    mOVEMENT.Items.Add(mOVEMENTITEM);
				}
				
                
                if (mOVEMENT.Items.Count > 0)
				{
					num++;
					(new MOVEMENT_BL()).Save(mOVEMENT);
					long dMOVEMENT = mOVEMENT.ID_MOVEMENT;
					this.filter = string.Format("{0}{1}", (string.IsNullOrEmpty(this.filter) ? string.Empty : string.Concat(this.filter, ", ")), dMOVEMENT);
				}
			}
			ePlus.MetaData.Core.Logger.ShowMessage(string.Format("Операция завершена.\r\nСоздано {0} документа(ов)", num), 0, MessageBoxIcon.Asterisk);
			base.Close();
			if (num > 0)
			{
				PluginFormView pluginView = AppManager.GetPluginView("Movement");
				if (pluginView != null)
				{
					AppManager.RegisterForm(pluginView, "");
					if (!string.IsNullOrEmpty(this.filter))
					{
						pluginView.Grid(0).SetParameterValue("@ADV_FILTER", string.Format("ID_MOVEMENT IN ({0})", this.filter));
					}
					pluginView.Show();
				}
			}
		}

		private void buttonCancel_Click(object sender, EventArgs e)
		{
			base.Close();
		}

		private void buttonRefreshCols_Click(object sender, EventArgs e)
		{
             //this.chStore.CheckedItems 2019
            this.TransformData();
		}

		private void buttonStore_Click(object sender, EventArgs e)
		{
			if (this.pluginBox1.RowItem.Id == (long)0)
			{
				return;
			}
			StockSelectWrapper stockSelectWrapper = new StockSelectWrapper();
			stockSelectWrapper.StockFilter.StoreId = this.pluginBox1.RowItem.Id;
			if (stockSelectWrapper.Show() == System.Windows.Forms.DialogResult.OK)
			{
				StockRecord stockRecord = StockSelectWrapper.GetStockRecord(stockSelectWrapper.GridController.SelectedRow());
				InvoiceAutoItem invoiceAutoItem = new InvoiceAutoItem()
				{
					Id_goods = stockRecord.IdGoods,
					Id_party = stockRecord.IdLot
				};
				foreach (InvoiceAutoItem item in this.invoiceAuto.Items)
				{
					if (item.Id_goods != invoiceAutoItem.Id_goods || item.Id_party != invoiceAutoItem.Id_party)
					{
						continue;
					}
					return;
				}
				GOODS_BL bL = (GOODS_BL)BLProvider.Instance.GetBL(typeof(GOODS_BL));
				invoiceAutoItem.Goods_name = ((GOODS)bL.Load(invoiceAutoItem.Id_goods)).NAME;
				LOT_BL lOTBL = (LOT_BL)BLProvider.Instance.GetBL(typeof(LOT_BL));
				LOT lOT = lOTBL.Load(invoiceAutoItem.Id_party);
				if (lOT.QUANTITY_REM <= new decimal(0))
				{
					return;
				}
				invoiceAutoItem.Store_quantity = lOT.QUANTITY_REM;
				invoiceAutoItem.Id_scale_ratio = lOT.ID_SCALING_RATIO;
				SCALING_RATIO_BL sCALINGRATIOBL = (SCALING_RATIO_BL)BLProvider.Instance.GetBL(typeof(SCALING_RATIO_BL));
				SCALING_RATIO sCALINGRATIO = (SCALING_RATIO)sCALINGRATIOBL.Load(invoiceAutoItem.Id_scale_ratio);
				invoiceAutoItem.Scale_ratio_name = string.Concat(sCALINGRATIO.SCALING_RATIO_TEXT, ' ', sCALINGRATIO.UNIT_NAME);
				this.plInvoice.SetId((long)0);
				this.invoiceAuto.Items.Add(invoiceAutoItem);
				this.TransformData();
				this.grdItems.BindingSource.CurrencyManager.Refresh();
			}
		}

		private void chStore_ItemCheck(object sender, ItemCheckEventArgs e)
		{
			if (((STORE)this.chStore.Items[e.Index]).ID_STORE == this.pluginBox1.Id || this.pluginBox1.Id == (long)0)
			{
				e.NewValue = CheckState.Unchecked;
			}
			this.chStore.ItemCheck -= new ItemCheckEventHandler(this.chStore_ItemCheck);
			this.chStore.SetItemChecked(e.Index, e.NewValue == CheckState.Checked);
			this.chStore.ItemCheck += new ItemCheckEventHandler(this.chStore_ItemCheck);
			this.TransformData();
		}

		private void delButton_Click(object sender, EventArgs e)
		{
			if (this.grdItems.DataGridView.RowCount > 0)
			{
				this.plInvoice.SetId((long)0);
				this.invoiceAuto.Items.RemoveAt(this.grdItems.DataGridView.CurrentRow.Index);
				this.TransformData();
			}
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing && this.components != null)
			{
				this.components.Dispose();
			}
			base.Dispose(disposing);
		}

		private void fromStore_Click(object sender, EventArgs e)
		{
           
            long IdStoreTo = 0;
            // нажимаем на выбор склада "Откуда"
            if (this.pluginBox1.RowItem.Id == (long)0)
			{
				return;
			}

            ///Заполняем содержимое таблицы ниже
            
            foreach (object itemChecked in this.chStore.CheckedItems)
            {

                STORE item = (STORE)itemChecked;
                IdStoreTo = item.ID_STORE;
                LogTrace("Нашли склад " +item.NAME ); // отладка
                break;
                #region кусок из заполнения документа
                //STORE item = (STORE)this.chStore.Items[i];
                /*MOVEMENT mOVEMENT = new MOVEMENT()
                {
                    MNEMOCODE = DOCUMENT_DALC.GetDocumentNumber((long)8),
                    DOCUMENT_STATE = (new DocumentState((EDocumentState)((long)1))).Mnemocode,
                    ID_STORE_FROM = this.pluginBox1.Id,
                    ID_STORE_TO = item.ID_STORE,
                    DATE = DateTime.Now
                };*/
                #endregion
            }


            this.plInvoice.SetId((long)0);
			InvoiceAuto_BL invoiceAutoBL = new InvoiceAuto_BL();
			this.invoiceAuto.Items.Clear(); // очистка таблицы
			 if (IdStoreTo > 0)
            {
                this.invoiceAuto.Items.AddRange(invoiceAutoBL.ListRest(this.pluginBox1.RowItem.Id,IdStoreTo));
            }
            else { this.invoiceAuto.Items.AddRange(invoiceAutoBL.ListRest(this.pluginBox1.RowItem.Id)); }
            
			
            
            this.TransformData();
		}

		private void grdItems_GridCellEndEdit(object sender, DataGridViewCellEventArgs e)
		{
			this.SyncItems();
		}

		private void InitializeComponent()
		{
			this.grdItems = new TableEditorControl();
			this.panel1 = new Panel();
			this.panel3 = new Panel();
			this.label3 = new Label();
			this.chStore = new CheckedListBox();
			this.panel4 = new Panel();
			this.label2 = new Label();
			this.label1 = new Label();
			this.plInvoice = new PluginBox();
			this.pluginBox1 = new PluginBox();
			this.fromStore = new Button();
			this.delButton = new Button();
			this.buttonStore = new Button();
			this.panel2 = new Panel();
			this.bMove = new Button();
			this.buttonCancel = new Button();
			this.splitter1 = new Splitter();
			this.buttonRefreshCols = new Button();
			this.panel1.SuspendLayout();
			this.panel3.SuspendLayout();
			this.panel4.SuspendLayout();
			this.panel2.SuspendLayout();
			base.SuspendLayout();
			this.grdItems.DataSource = null;
			this.grdItems.Dock = DockStyle.Fill;
			this.grdItems.Location = new Point(0, 111);
			this.grdItems.Mnemocode = "INVOICE_AUTO";
			this.grdItems.Name = "grdItems";
			this.grdItems.ObjectList = null;
			this.grdItems.Size = new System.Drawing.Size(816, 192);
			this.grdItems.TabIndex = 30;
			this.grdItems.GridCellEndEdit += new DataGridViewCellEventHandler(this.grdItems_GridCellEndEdit);
			this.panel1.Controls.Add(this.panel3);
			this.panel1.Controls.Add(this.panel4);
			this.panel1.Dock = DockStyle.Top;
			this.panel1.Location = new Point(0, 0);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(816, 108);
			this.panel1.TabIndex = 31;
			this.panel3.Controls.Add(this.label3);
			this.panel3.Controls.Add(this.chStore);
			this.panel3.Dock = DockStyle.Fill;
			this.panel3.Location = new Point(368, 0);
			this.panel3.Name = "panel3";
			this.panel3.Size = new System.Drawing.Size(448, 108);
			this.panel3.TabIndex = 36;
			this.label3.AutoSize = true;
			this.label3.Location = new Point(3, 6);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(57, 13);
			this.label3.TabIndex = 2;
			this.label3.Text = "На склад:";
			this.chStore.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
			this.chStore.CheckOnClick = true;
			this.chStore.FormattingEnabled = true;
			this.chStore.Location = new Point(6, 23);
			this.chStore.Name = "chStore";
			this.chStore.Size = new System.Drawing.Size(434, 79);
			this.chStore.TabIndex = 0;
			this.chStore.ItemCheck += new ItemCheckEventHandler(this.chStore_ItemCheck);
			this.panel4.Controls.Add(this.label2);
			this.panel4.Controls.Add(this.buttonRefreshCols);
			this.panel4.Controls.Add(this.label1);
			this.panel4.Controls.Add(this.plInvoice);
			this.panel4.Controls.Add(this.pluginBox1);
			this.panel4.Dock = DockStyle.Left;
			this.panel4.Location = new Point(0, 0);
			this.panel4.Name = "panel4";
			this.panel4.Size = new System.Drawing.Size(368, 108);
			this.panel4.TabIndex = 0;
			this.label2.AutoSize = true;
			this.label2.Location = new Point(12, 43);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(66, 13);
			this.label2.TabIndex = 42;
			this.label2.Text = "Накладная:";
			this.label1.AutoSize = true;
			this.label1.Location = new Point(12, 12);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(62, 13);
			this.label1.TabIndex = 0;
			this.label1.Text = "Со склада:";
			this.plInvoice.CodeMaxLength = 32767;
			this.plInvoice.CodeText = "";
			this.plInvoice.CodeWidth = 0;
			this.plInvoice.ColorTextCode = SystemColors.WindowText;
			this.plInvoice.ColorTextText = SystemColors.WindowText;
			this.plInvoice.LikeTextOption = ELikeTextOption.MaskLeft;
			this.plInvoice.Location = new Point(80, 38);
			this.plInvoice.Mask = "";
			this.plInvoice.Mnemocode = "INVOICE";
			this.plInvoice.Name = "plInvoice";
			this.plInvoice.SelectOnSpaceKey = true;
			this.plInvoice.Size = new System.Drawing.Size(282, 23);
			this.plInvoice.TabIndex = 1;
			this.plInvoice.ValueChanged += new PluginBox.ValueChangedDelegate(this.plInvoice_ValueChanged);
			this.plInvoice.ValidatingItem += new PluginBox.ValidatingItemDelegate(this.plInvoice_ValidatingItem);
			this.pluginBox1.CodeMaxLength = 32767;
			this.pluginBox1.CodeText = "";
			this.pluginBox1.CodeWidth = 50;
			this.pluginBox1.ColorTextCode = SystemColors.WindowText;
			this.pluginBox1.ColorTextText = SystemColors.WindowText;
			this.pluginBox1.LikeTextOption = ELikeTextOption.None;
			this.pluginBox1.Location = new Point(80, 9);
			this.pluginBox1.Mask = "";
			this.pluginBox1.Mnemocode = "STORE";
			this.pluginBox1.Name = "pluginBox1";
			this.pluginBox1.SelectOnSpaceKey = true;
			this.pluginBox1.Size = new System.Drawing.Size(282, 23);
			this.pluginBox1.TabIndex = 0;
			this.pluginBox1.ValueChanged += new PluginBox.ValueChangedDelegate(this.pluginBox1_ValueChanged);
			this.fromStore.Location = new Point(12, 6);
			this.fromStore.Name = "fromStore";
			this.fromStore.Size = new System.Drawing.Size(87, 23);
			this.fromStore.TabIndex = 5;
			this.fromStore.Text = "Все остатки";
			this.fromStore.UseVisualStyleBackColor = true;
			this.fromStore.Click += new EventHandler(this.fromStore_Click);
			this.delButton.Location = new Point(222, 6);
			this.delButton.Name = "delButton";
			this.delButton.Size = new System.Drawing.Size(75, 23);
			this.delButton.TabIndex = 39;
			this.delButton.Text = "Удалить";
			this.delButton.UseVisualStyleBackColor = true;
			this.delButton.Click += new EventHandler(this.delButton_Click);
			this.buttonStore.Location = new Point(105, 6);
			this.buttonStore.Name = "buttonStore";
			this.buttonStore.Size = new System.Drawing.Size(111, 23);
			this.buttonStore.TabIndex = 37;
			this.buttonStore.Text = "Подбор позиции";
			this.buttonStore.UseVisualStyleBackColor = true;
			this.buttonStore.Click += new EventHandler(this.buttonStore_Click);
			this.panel2.Controls.Add(this.fromStore);
			this.panel2.Controls.Add(this.bMove);
			this.panel2.Controls.Add(this.buttonCancel);
			this.panel2.Controls.Add(this.buttonStore);
			this.panel2.Controls.Add(this.delButton);
			this.panel2.Dock = DockStyle.Bottom;
			this.panel2.Location = new Point(0, 303);
			this.panel2.Name = "panel2";
			this.panel2.Size = new System.Drawing.Size(816, 35);
			this.panel2.TabIndex = 32;
			this.bMove.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			this.bMove.Location = new Point(623, 6);
			this.bMove.Name = "bMove";
			this.bMove.Size = new System.Drawing.Size(101, 23);
			this.bMove.TabIndex = 0;
			this.bMove.Text = "Создать док.";
			this.bMove.UseVisualStyleBackColor = true;
			this.bMove.Click += new EventHandler(this.bMove_Click);
			this.buttonCancel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.buttonCancel.Location = new Point(730, 5);
			this.buttonCancel.Name = "buttonCancel";
			this.buttonCancel.Size = new System.Drawing.Size(76, 24);
			this.buttonCancel.TabIndex = 1;
			this.buttonCancel.Text = "Отмена";
			this.buttonCancel.UseVisualStyleBackColor = true;
			this.buttonCancel.Click += new EventHandler(this.buttonCancel_Click);
			this.splitter1.Dock = DockStyle.Top;
			this.splitter1.Location = new Point(0, 108);
			this.splitter1.Name = "splitter1";
			this.splitter1.Size = new System.Drawing.Size(816, 3);
			this.splitter1.TabIndex = 33;
			this.splitter1.TabStop = false;
			this.buttonRefreshCols.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
			this.buttonRefreshCols.Location = new Point(266, 78);
			this.buttonRefreshCols.Name = "buttonRefreshCols";
			this.buttonRefreshCols.Size = new System.Drawing.Size(76, 24);
			this.buttonRefreshCols.TabIndex = 1;
			this.buttonRefreshCols.Text = "Расчет";
			this.buttonRefreshCols.UseVisualStyleBackColor = true;
			this.buttonRefreshCols.Visible = true;
			this.buttonRefreshCols.Click += new EventHandler(this.buttonRefreshCols_Click);
			base.AutoScaleDimensions = new SizeF(6f, 13f);
			base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			base.ClientSize = new System.Drawing.Size(816, 338);
			base.Controls.Add(this.grdItems);
			base.Controls.Add(this.splitter1);
			base.Controls.Add(this.panel2);
			base.Controls.Add(this.panel1);
			base.MaximizeBox = true;
			base.MinimizeBox = true;
			this.MinimumSize = new System.Drawing.Size(670, 365);
			base.Name = "InvoiceAutoForm";
			this.Text = "Распределение товара";
			base.Load += new EventHandler(this.InvoiceAutoForm_Load);
			this.panel1.ResumeLayout(false);
			this.panel3.ResumeLayout(false);
			this.panel3.PerformLayout();
			this.panel4.ResumeLayout(false);
			this.panel4.PerformLayout();
			this.panel2.ResumeLayout(false);
			base.ResumeLayout(false);
		}

		private void InvoiceAutoForm_Load(object sender, EventArgs e)
		{
			this.storeDict.Clear();
			ePlus.Dictionary.Server.STORE_BL bL = (ePlus.Dictionary.Server.STORE_BL)BLProvider.Instance.GetBL(typeof(ePlus.Dictionary.Server.STORE_BL));
			
            ArrayList arrayLists = bL.List("DATE_DELETED IS NULL");
			for (int i = 0; i < arrayLists.Count; i++)
			{
				STORE item = (STORE)arrayLists[i];
				this.storeDict.Add(i, item.NAME);
				this.chStore.Items.Add(item, false);
			}
			this.grdItems.ContextMenuGrid.Items.Clear();
			this.TransformData();
		}

		private void OnCellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
		{
			Color red;
			if (e.RowIndex < 0 || e.RowIndex >= this.grdItems.DataGridView.RowCount)
			{
				return;
			}
			if (e.ColumnIndex < 0 || e.ColumnIndex >= this.grdItems.DataGridView.ColumnCount)
			{
				return;
			}
			DataTable dataSource = this.grdItems.DataSource as DataTable;
			if (dataSource == null)
			{
				return;
			}
			if (e.RowIndex < 0 || e.RowIndex >= dataSource.Rows.Count)
			{
				return;
			}
			if (e.ColumnIndex < 0 || e.ColumnIndex >= dataSource.Columns.Count)
			{
				return;
			}
			DataRow item = dataSource.Rows[e.RowIndex];
			if (dataSource.Columns[e.ColumnIndex].ColumnName == "LEFT_QUANTITY")
			{
				decimal num = (decimal)item[e.ColumnIndex];
				DataGridViewCellStyle cellStyle = e.CellStyle;
				if (num < new decimal(0))
				{
					red = Color.Red;
				}
				else
				{
					red = (num > new decimal(0) ? Color.Green : Color.Blue);
				}
				cellStyle.ForeColor = red;
			}
		}

		private void OnCurrentCellChanged(object sender, EventArgs e)
		{
		}

		private void OnEditorShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
		{
			e.CellStyle.BackColor = Color.Yellow;
			e.Control.KeyPress += new KeyPressEventHandler(this.OnKeyPress);
		}

		private void OnKeyPress(object sender, KeyPressEventArgs e)
		{
			if (!char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar))
			{
				e.Handled = true;
			}
		}

		private bool plInvoice_ValidatingItem(DataRowItem item)
		{
			if (item.Id == (long)0)
			{
				return true;
			}
			INVOICE nVOICE = (new INVOICE_BL()).Load(item.Guid);
			if (this.pluginBox1.RowItem.Id == (long)0)
			{
				this.pluginBox1.SetId(nVOICE.ID_STORE);
				return true;
			}
			if (nVOICE.ID_STORE == this.pluginBox1.Id)
			{
				return true;
			}
			ePlus.MetaData.Core.Logger.ShowInfo("Накладная принадлежит другому складу!");
			return false;
		}

		private void plInvoice_ValueChanged()
		{
			if (this.plInvoice.RowItem.Id == (long)0)
			{
				return;
			}
			InvoiceAuto_BL invoiceAutoBL = new InvoiceAuto_BL();
			this.invoiceAuto.Items.Clear();
			this.invoiceAuto.Items.AddRange(invoiceAutoBL.ListLot(this.plInvoice.RowItem.Guid));
			this.TransformData();
		}

		private void pluginBox1_ValueChanged()
		{
			this.invoiceAuto.Items.Clear();
			this.grdItems.DataGridView.ClearSelection();
			this.grdItems.BindingSource.CurrencyManager.Refresh();
			this.plInvoice.SetId((long)0);
			this.plInvoice.Text = "";
			for (int i = 0; i < this.chStore.Items.Count; i++)
			{
				this.chStore.SetItemCheckState(i, CheckState.Unchecked);
			}
			this.TransformData();
		}

		private void SyncItems()
		{
			int num;
			DataTable dataSource = this.grdItems.DataSource as DataTable;
			if (dataSource != null)
			{
				foreach (DataRow row in dataSource.Rows)
				{
					InvoiceAutoItem item = this.invoiceAuto.Items.Find((InvoiceAutoItem it) => it.Id_party == (long)row["ID_LOT"]);
					if (item == null)
					{
						continue;
					}
					foreach (DataColumn column in dataSource.Columns)
					{
						string columnName = column.ColumnName;
						string str = columnName;
						if (columnName != null && (str == "ID_LOT" || str == "GOODS_NAME" || str == "SCALING" || str == "SOLD" || str == "REMAIN" || str == "STORE_QUANTITY" || str == "LEFT_QUANTITY") || !int.TryParse(column.ColumnName, out num) || num < 0 || num >= item.Quantities.Count)
						{
							continue;
						} 
						item.Quantities[num] = (decimal)row[column];
					}
				}
			}
			foreach (InvoiceAutoItem invoiceAutoItem in this.invoiceAuto.Items)
			{
				DataRow leftQuantity = dataSource.Rows.Find(invoiceAutoItem.Id_party);
				if (leftQuantity == null)
				{
					continue;
				}
				for (int i = 0; i < invoiceAutoItem.Quantities.Count; i++)
				{
					if (!this.chStore.GetItemChecked(i))
					{
						invoiceAutoItem.Quantities[i] = new decimal(0);
					}
					else
					{
						leftQuantity[i.ToString()] = invoiceAutoItem.Quantities[i];
					}
				}
				leftQuantity["LEFT_QUANTITY"] = invoiceAutoItem.Left_quantity;
			}
			this.grdItems.BindingSource.ResetCurrentItem();
		}

        /// <summary>
        /// Расчет заказа для одной аптеки
        /// Добиваем остатки до месяца 2019
        /// </summary>
        private void Zakaz ()
        {

            int storeidx=0;
            for (int i = 0; i < this.chStore.Items.Count; i++)
            {

             if (this.chStore.GetItemCheckState(i) == CheckState.Checked)
                {
                    storeidx = i;
                }
            }


                String GoodName = "";
            int remain = 0;
            int sold = 0;
            int zakaz = 0;
          
            foreach (InvoiceAutoItem item in this.invoiceAuto.Items)
            {
                item.Quantities.Clear();
                for (int i = 0; i < this.chStore.Items.Count; i++)
                {

                    item.Quantities.Add(new decimal(0));

                }
                if (GoodName!=item.Goods_name)
                {
                    //новая строка для расчета 
                    GoodName = item.Goods_name;
                    remain =(int) item.Remain;
                    sold = (int)item.Sold;
                    if (remain < sold)
                    { zakaz =  sold - remain; }
                    else
                    { zakaz = 0; }
                      

                }
                if (zakaz>0 && item.Scale_ratio_name.Contains("1/1")) // распределяем только целые упаковки
                {
                    if (item.Store_quantity >= zakaz)
                    {
                        //item.Quantities.Add(zakaz);
                        item.Quantities[storeidx] = zakaz;
                        zakaz = 0;
                    }
                    else
                    {
                        item.Quantities[storeidx]=item.Store_quantity;
                        zakaz = zakaz -(int)item.Store_quantity;
                    }
                }
                
            }
        }

		private void TransformData()
		{
			int num;
			int num1;
			foreach (InvoiceAutoItem item in this.invoiceAuto.Items)
			{
				item.Quantities.Clear();
				for (int i = 0; i < this.chStore.Items.Count; i++)
				{
					item.Quantities.Add(new decimal(0));
				}
			}

            /// 2019
            /// простой расчет заказа
            Zakaz(); 


			DataTable dataSource = this.grdItems.DataSource as DataTable;
			if (dataSource != null)
			{
				foreach (DataRow row in dataSource.Rows)
				{
					InvoiceAutoItem invoiceAutoItem = this.invoiceAuto.Items.Find((InvoiceAutoItem it) => it.Id_party == (long)row["ID_LOT"]);
					if (invoiceAutoItem == null)
					{
						continue;
					}
					foreach (DataColumn column in dataSource.Columns)
					{
						string columnName = column.ColumnName;
						string str = columnName;
						if (columnName != null && (str == "ID_LOT" || str == "GOODS_NAME" || str == "SCALING" || str == "REMAIN" || str == "SOLD" || str == "STORE_QUANTITY" || str == "LEFT_QUANTITY") || !int.TryParse(column.ColumnName, out num) || num < 0 || num >= invoiceAutoItem.Quantities.Count)
						{
							continue;
						}
						invoiceAutoItem.Quantities[num] = (decimal)row[column];
					}
				}
			}
			DataTable dataTable = new DataTable();
			DataColumn dataColumn = dataTable.Columns.Add("ID_LOT", typeof(long));
			dataColumn.ReadOnly = true;
			dataTable.PrimaryKey = new DataColumn[] { dataColumn };
			dataTable.Columns.Add("GOODS_NAME", typeof(string)).ReadOnly = true;
			dataTable.Columns.Add("SCALING", typeof(string)).ReadOnly = true;
			dataTable.Columns.Add("STORE_QUANTITY", typeof(decimal)).ReadOnly = true;
			dataTable.Columns.Add("LEFT_QUANTITY", typeof(decimal));

            /*2019*/
            dataTable.Columns.Add("SOLD", typeof(decimal)).ReadOnly = true; //  продано за месяц в аптеке
            dataTable.Columns.Add("REMAIN", typeof(decimal)).ReadOnly = true;// сколько осталось в аптеке
            /*2019*/
            if (this.invoiceAuto.Items.Count > 0)
			{
				InvoiceAutoItem item1 = this.invoiceAuto.Items[0];
				for (int j = 0; j < item1.Quantities.Count; j++)
				{
					if (this.chStore.GetItemChecked(j))
					{
						dataTable.Columns.Add(j.ToString(), typeof(decimal));
					}
				}
			}
			this.grdItems.DataGridView.Columns.Clear();
			this.grdItems.DataSource = dataTable;
			foreach (DataColumn column1 in dataTable.Columns)
			{
				DataGridViewColumn dataGridViewColumn = null;
				if (column1.DataType == typeof(string))
				{
					DataGridViewTextBoxCell2 dataGridViewTextBoxCell2 = new DataGridViewTextBoxCell2()
					{
						ValueType = typeof(string)
					};
					dataGridViewColumn = new DataGridViewColumn(dataGridViewTextBoxCell2)
					{
						Tag = new MetaGeColumnString()
					};
				}
				if (column1.DataType == typeof(decimal) || column1.DataType == typeof(long))
				{
					dataGridViewColumn = new DataGridViewColumn(new DataGridViewNumericCell());
					dataGridViewColumn.CellTemplate.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
					dataGridViewColumn.CellTemplate.Style.Format = "N0";
					dataGridViewColumn.Tag = new MetaGeColumnNumeric();
				}
				if (dataGridViewColumn == null)
				{
					continue;
				}
				dataGridViewColumn.DataPropertyName = column1.ColumnName;
				this.grdItems.DataGridView.Columns.Add(dataGridViewColumn);
			}
			foreach (DataGridViewColumn dataGridViewColumn1 in this.grdItems.DataGridView.Columns)
			{
				string dataPropertyName = dataGridViewColumn1.DataPropertyName;
				string str1 = dataPropertyName;
				if (dataPropertyName != null)
				{
					if (str1 == "ID_LOT")
					{
						dataGridViewColumn1.Visible = false;
						continue;
					}
					else if (str1 == "GOODS_NAME")
					{
						dataGridViewColumn1.HeaderText = "Товар";
						dataGridViewColumn1.DisplayIndex = 0;
						dataGridViewColumn1.Frozen = true;
						continue;
					}
					else if (str1 == "SCALING")
					{
						dataGridViewColumn1.HeaderText = "Единица измерения";
						dataGridViewColumn1.DisplayIndex = 1;
						dataGridViewColumn1.Frozen = true;
						continue;
					}
					else if (str1 == "STORE_QUANTITY")
					{
						dataGridViewColumn1.HeaderText = "На складе";
						dataGridViewColumn1.DisplayIndex = 2;
						dataGridViewColumn1.Frozen = true;
						continue;
					}
					else if (str1 == "LEFT_QUANTITY")
					{
						dataGridViewColumn1.HeaderText = "Не распределено";
						dataGridViewColumn1.DisplayIndex = 3;
						dataGridViewColumn1.Frozen = true;
						dataGridViewColumn1.ReadOnly = true;
						continue;
					}
                    else if (str1 == "SOLD")
                    {
                        dataGridViewColumn1.HeaderText = "Продано";
                        dataGridViewColumn1.DisplayIndex = 4;
                        dataGridViewColumn1.Frozen = true;
                        dataGridViewColumn1.ReadOnly = true;
                        continue;
                    }
                    else if (str1 == "REMAIN")
                    {
                        dataGridViewColumn1.HeaderText = "Остаток в аптеке";
                        dataGridViewColumn1.DisplayIndex = 5;
                        dataGridViewColumn1.Frozen = true;
                        dataGridViewColumn1.ReadOnly = true;
                        continue;
                    }
                }
				//название склада в заголовок колонки
                if (!int.TryParse(dataGridViewColumn1.DataPropertyName, out num1) || !this.storeDict.ContainsKey(num1))
				{
					continue;
				}
				dataGridViewColumn1.HeaderText = this.storeDict[num1];
			}
			foreach (InvoiceAutoItem invoiceAutoItem1 in this.invoiceAuto.Items)
			{
				DataRow idParty = dataTable.NewRow();
				idParty["ID_LOT"] = invoiceAutoItem1.Id_party;
				idParty["GOODS_NAME"] = invoiceAutoItem1.Goods_name;
				idParty["SCALING"] = invoiceAutoItem1.Scale_ratio_name;
				idParty["STORE_QUANTITY"] = invoiceAutoItem1.Store_quantity;
                
                idParty["SOLD"] = invoiceAutoItem1.Sold;
                idParty["REMAIN"] = invoiceAutoItem1.Remain;

                for (int k = 0; k < invoiceAutoItem1.Quantities.Count; k++)
				{
					if (!this.chStore.GetItemChecked(k))
					{
						invoiceAutoItem1.Quantities[k] = new decimal(0);
					}
					else
					{
						idParty[k.ToString()] = invoiceAutoItem1.Quantities[k];
					}
				}
				idParty["LEFT_QUANTITY"] = invoiceAutoItem1.Left_quantity;
				dataTable.Rows.Add(idParty);
			}
			this.grdItems.BindingSource.ResetBindings(true);
			this.grdItems.DataGridView.AutoResizeColumns();
		}

		private bool ValidateDocument()
		{
			if (this.pluginBox1.Id == (long)0)
			{
				return false;
			}
			if (this.invoiceAuto.Items.Count == 0)
			{
				return false;
			}
			if (this.invoiceAuto.Items[0].Quantities.Count == 0)
			{
				return false;
			}
			return true;
		}
	}
}