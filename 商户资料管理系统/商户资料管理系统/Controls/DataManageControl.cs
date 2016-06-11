﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using 商户资料管理系统.Common;
using 商户资料管理系统.YatServer;
using 商户资料管理系统.Properties;
using System.IO;

namespace 商户资料管理系统
{
    public partial class DataManageControl : UserControl
    {
        private static YatMingServiceClient _client = ServiceProvider.Clent;
        private string _baseInfoId = string.Empty;
        private string _currentId = string.Empty;
        private string _oldText = string.Empty;

        public DataManageControl()
        {
            InitializeComponent();          
        }

        public void InitializeContent(string id)
        {
            LvDataContent.InsertionMark.Color = Color.Red;
            LvDataContent.ListViewItemSorter = new ListViewIndexComparer();
            _baseInfoId = id;
            IniliazeListView(null);
        }

        private void IniliazeListView(string pid)
        {
            TDataInfoDTO[] allResult = null;
            LvDataContent.Items.Clear();
            _currentId = pid;
            //根节点
            if (string.IsNullOrWhiteSpace(pid))
            {
                allResult = _client.TDataInGetByForginKey(_baseInfoId).Where(t => string.IsNullOrWhiteSpace(t.ParentId)).ToArray();
                tsbReturn.Enabled = false;
            }
            else
            {
                tsbReturn.Enabled = true;
                allResult = _client.TDataInGetByParentKey(pid);
            }
            Array.ForEach(allResult, d =>
            {
                CreateViewItem(d, LvDataContent.Items.Count);
            });
        }

        #region 操作Listview

        private void CreateViewItem(TDataInfoDTO dto,int index)
        {
            ListViewItemEx lvi = new ListViewItemEx();
            lvi.ItemData = dto;
            lvi.Text = dto.DataName;
            if (dto.IsForlder == false)
            {
                string[] arrtempFileName = dto.DataName.Split(new char[] { '.' });
                string tempFileExtension = "." + arrtempFileName[arrtempFileName.Length - 1];
                if (!imageList1.Images.Keys.Contains(tempFileExtension))
                    imageList1.Images.Add(tempFileExtension, IconsExtention.IconFromExtension(tempFileExtension, IconsExtention.SystemIconSize.Large));
                lvi.ImageIndex = imageList1.Images.Keys.IndexOf(tempFileExtension);
                lvi.ToolTipText = string.Format("文件名称:{0}\r\n文件大小:{1}M\r\n上传时间:{2}\r\n上传人:{3}\r\n修改时间:{4}\r\n下载次数:{5}\r\n文件描述:{6}", dto.DataName, CommomHelper.ParseMB(dto.FileSize), dto.CreateTime, dto.UploadPeople, dto.LastModifyTime, dto.DownloadTimes, dto.DataDescription);
                LvDataContent.Items.Insert(index,lvi);
                lvi.SetOtherControl();
            }
            else
            {
                if (!imageList1.Images.Keys.Contains("Folder"))
                    imageList1.Images.Add("Folder", Resources.folder);
                lvi.ImageIndex = imageList1.Images.Keys.IndexOf("Folder");
                string.Format("文件名称:{0}\r\n上传时间:{1}\r\n上传人:{2}\r\n修改时间:{3}\r\n文件描述:{4}", dto.DataName, dto.CreateTime, dto.UploadPeople, dto.LastModifyTime, dto.DataDescription);
                LvDataContent.Items.Insert(index, lvi);
            }
        }

        private void UpdateViewItem(TDataInfoDTO dto, ListViewItemEx lvi)
        {
            lvi.Text = dto.DataName;
            lvi.ItemData = dto;
            if (dto.IsForlder == false)
            {
                string[] arrtempFileName = dto.DataName.Split(new char[] { '.' });
                string tempFileExtension = "." + arrtempFileName[arrtempFileName.Length - 1];
                //get imageindex from imagelist according to the file extension  
                if (!imageList1.Images.Keys.Contains(tempFileExtension))
                    imageList1.Images.Add(tempFileExtension, IconsExtention.IconFromExtension(tempFileExtension, IconsExtention.SystemIconSize.Large));
                lvi.ImageIndex = imageList1.Images.Keys.IndexOf(tempFileExtension);
                lvi.ToolTipText = string.Format("文件名称:{0}\r\n文件大小:{1}\r\n上传时间:{2}\r\n上传人:{3}\r\n修改时间:{4}\r\n下载次数:{5}\r\n文件描述:{6}", dto.DataName, dto.FileSize, dto.CreateTime, dto.UploadPeople, dto.LastModifyTime, dto.DownloadTimes, dto.DataDescription);
            }
            else
            {
                if (!imageList1.Images.Keys.Contains("Folder"))
                    imageList1.Images.Add("Folder", Resources.folder);
                lvi.ImageIndex = imageList1.Images.Keys.IndexOf("Folder");
                string.Format("文件名称:{0}\r\n上传时间:{1}\r\n上传人:{2}\r\n修改时间:{3}\r\n文件描述:{4}", dto.DataName, dto.CreateTime, dto.UploadPeople, dto.LastModifyTime, dto.DataDescription);
            }
        }

        #endregion

        private void tsbReturn_Click(object sender, EventArgs e)
        {
            TDataInfoDTO dto = _client.TDataInfoQueryById(_currentId);
            string parentId = dto.ParentId;
            IniliazeListView(parentId);
        }

        private void tsbRefresh_Click(object sender, EventArgs e)
        {
            IniliazeListView(_currentId);
        }

        private void tsbNewForlder_Click(object sender, EventArgs e)
        {
            TDataInfoDTO dto = new TDataInfoDTO();
            dto.BaseInfoId = _baseInfoId;
            dto.CreateTime = DateTime.Now;
            dto.DataName = "新建文件夹";
            dto.UploadPeople = CommonData.LoginInfo.EmployeeName;
            dto.IsForlder = true;
            dto.DownloadTimes = 0;
            dto.LastModifyTime = DateTime.Now;
            dto.MetaDataId = Guid.NewGuid().ToString();
            dto.ParentId = _currentId;
            bool result = _client.TDataInfoAdd(dto);
            if (result)
                CreateViewItem(dto, LvDataContent.Items.Count);
        }

        private void tsbDelete_Click(object sender, EventArgs e)
        {
            ListView.SelectedListViewItemCollection lstSelected = LvDataContent.SelectedItems;
            if (lstSelected.Count > 0)
            {
                foreach (ListViewItem item in LvDataContent.SelectedItems)
                {
                    TDataInfoDTO dto = (item as ListViewItemEx).ItemData;
                    //删除文件夹
                    if (dto.IsForlder)
                    {
                        TDataInfoDTO[] result = _client.TDataInGetByParentKey(dto.MetaDataId);
                        if (result != null && result.Length > 0)
                            Array.ForEach(result, t => { _client.TDataInfoDelete(t.MetaDataId); });
                    }
                    bool success = _client.TDataInfoDelete(dto.MetaDataId);
                    if (success)
                        LvDataContent.Items.Remove(item);
                }
            }
        }

        private void tsbMoveTo_Click(object sender, EventArgs e)
        {

        }

        #region 下载

        private void tsbDownload_Click(object sender, EventArgs e)
        {
            ListView.SelectedListViewItemCollection lstSelected = LvDataContent.SelectedItems;
            if (lstSelected.Count > 0)
            {
                foreach (ListViewItem item in lstSelected)
                {
                    ListViewItemEx ctr = item as ListViewItemEx;
                    if (!ctr.ItemData.IsForlder)
                        ctr.DownLoadFile();
                }
            }
        }

        #endregion

        #region 编辑

        private void LvDataContent_AfterLabelEdit(object sender, LabelEditEventArgs e)
        {
            string newText = e.Label;
            if (string.IsNullOrEmpty(newText) || newText == _oldText)
            {
                e.CancelEdit = true;
                return;
            }
            TDataInfoDTO dto = (LvDataContent.Items[e.Item] as ListViewItemEx).ItemData;
            dto.DataName = newText;
            dto.LastModifyTime = DateTime.Now;
            dto.ParentId = dto.ParentId == null ? string.Empty : dto.ParentId;
            bool success = _client.TDataInfoUpdate(dto);
            if (success)
                UpdateViewItem(dto, LvDataContent.Items[e.Item] as ListViewItemEx);
        }

        private void LvDataContent_BeforeLabelEdit(object sender, LabelEditEventArgs e)
        {
            _oldText = e.Label;
        }

        #endregion

        #region 拖拽

        /// <summary>
        /// 当拖动某项时触发
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void listView1_ItemDrag(object sender, ItemDragEventArgs e)
        {
            Dictionary<ListViewItemEx, int> itemsCopy = new Dictionary<ListViewItemEx, int>();
            foreach (ListViewItemEx item in LvDataContent.SelectedItems)
                itemsCopy.Add(item, item.Index);
            LvDataContent.DoDragDrop(itemsCopy, DragDropEffects.Move);
        }

        private void LvDataContent_DragLeave(object sender, EventArgs e)
        {
            LvDataContent.InsertionMark.Index = -1;
        }

        /// <summary>
        /// 鼠标拖动某项至该控件的区域
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void listView1_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = e.AllowedEffect;
            //if (e.AllowedEffect == DragDropEffects.Move)
            //    e.Effect = DragDropEffects.Move;
            //else
            //    e.Effect = DragDropEffects.Copy;
        }

        /// <summary>
        /// 拖动时拖着某项置于某行上方时
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void listView1_DragOver(object sender, DragEventArgs e)
        {
            // 获得鼠标坐标
            Point point = LvDataContent.PointToClient(new Point(e.X, e.Y));
            // 返回离鼠标最近的项目的索引
            int index = LvDataContent.InsertionMark.NearestIndex(point);
            // 确定光标不在拖拽项目上
            if (index > -1)
            {
                Rectangle itemBounds = LvDataContent.GetItemRect(index);
                if (point.X > itemBounds.Left + (itemBounds.Width / 2))
                {
                    LvDataContent.InsertionMark.AppearsAfterItem = true;
                }
                else
                {
                    LvDataContent.InsertionMark.AppearsAfterItem = false;
                }
            }
            LvDataContent.InsertionMark.Index = index;
        }

        /// <summary>
        /// 结束拖动
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void listView1_DragDrop(object sender, DragEventArgs e)
        {
            // 返回插入标记的索引值  
            int index = LvDataContent.InsertionMark.Index;
            // 如果插入标记不可见，则退出.  
            if (index == -1)
            {
                return;
            }
            // 如果插入标记在项目的右面，使目标索引值加一  
            if (LvDataContent.InsertionMark.AppearsAfterItem && index < LvDataContent.Items.Count - 1 && (LvDataContent.Items[index] as ListViewItemEx).ItemData.IsForlder == false)
            {
                index++;
            }
            ListViewItemEx target = LvDataContent.Items[index] as ListViewItemEx;
            //移动项
            if (e.Effect == DragDropEffects.Move)
            {
                // 返回拖拽项  
                Dictionary<ListViewItemEx, int> items = (Dictionary<ListViewItemEx, int>)e.Data.GetData(typeof(Dictionary<ListViewItemEx, int>));
                foreach (var item in items)
                {
                    if (target.ItemData.IsForlder == false)
                    {
                        CreateViewItem(item.Key.ItemData, index);
                        LvDataContent.Items.Remove(item.Key);
                        if (item.Value >= index) index++;
                    }
                    else
                    {
                        TDataInfoDTO sourceDTO = item.Key.ItemData;
                        sourceDTO.ParentId = target.ItemData.MetaDataId;
                        bool success = _client.TDataInfoUpdate(sourceDTO);
                        if (success)
                        {
                            LvDataContent.Items.Remove(item.Key);
                        }
                    }
                }
            }
            else if (e.Effect == (DragDropEffects.Copy | DragDropEffects.Link | DragDropEffects.Move))
            {
                if (target.ItemData.IsForlder)
                {
                    IniliazeListView(target.ItemData.MetaDataId);
                }
                //上传文件
                string[] result = e.Data.GetData(DataFormats.FileDrop) as string[];
                Array.ForEach(result, t =>
                {

                    ListViewItemEx ctr = new ListViewItemEx(_baseInfoId);
                    ctr.Text = Path.GetFileName(t);
                    string tempFileExtension = Path.GetExtension(t); 
                    if (!imageList1.Images.Keys.Contains(tempFileExtension))
                        imageList1.Images.Add(tempFileExtension, IconsExtention.IconFromExtension(tempFileExtension, IconsExtention.SystemIconSize.Large));
                    ctr.ImageIndex = imageList1.Images.Keys.IndexOf(tempFileExtension);
                    LvDataContent.Items.Add(ctr);
                    ctr.SetOtherControl();
                    ctr.UploadFile(t, _currentId);
                });
            }

        }

        #endregion

        #region 上传

        private void tsbUpload_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "所有文件|*.*";
            dialog.Multiselect = true;
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                Array.ForEach(dialog.FileNames, t =>
                {
                    ListViewItemEx ctr = new ListViewItemEx(_baseInfoId);
                    ctr.Text = Path.GetFileName(t);
                    string tempFileExtension = Path.GetExtension(t);
                    //get imageindex from imagelist according to the file extension  
                    if (!imageList1.Images.Keys.Contains(tempFileExtension))
                        imageList1.Images.Add(tempFileExtension, IconsExtention.IconFromExtension(tempFileExtension, IconsExtention.SystemIconSize.Large));
                    ctr.ImageIndex = imageList1.Images.Keys.IndexOf(tempFileExtension);

                    LvDataContent.Items.Add(ctr);
                    ctr.SetOtherControl();
                    ctr.UploadFile(t, _currentId);
                });
            }
        }

        #endregion

        #region 打开

        private void LvDataContent_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                if (LvDataContent.SelectedItems.Count == 1)
                {
                    TDataInfoDTO dto = (LvDataContent.SelectedItems[0] as ListViewItemEx).ItemData;
                    if (dto.IsForlder)
                    {
                        IniliazeListView(dto.MetaDataId);
                    }
                    else
                    {
                        //打开
                        FormView view = new FormView();
                        view.Show();
                        view.View(dto.MetaDataId);
                    }
                }
            }
        }

        #endregion

        #region 搜索

        private void searchTextBox1_OnSearch(object sender, SearchEventArgs e)
        {
            TDataInfoDTO[] result = null;

            if (string.IsNullOrEmpty(e.SearchText))
                result = _client.TDataInGetByForginKey(_baseInfoId);
            else
                result = _client.TDataInQuery(false, DateTime.Now, DateTime.Now, e.SearchText, _baseInfoId);
            LvDataContent.Items.Clear();
            Array.ForEach(result, t =>
            {
                CreateViewItem(t, LvDataContent.Items.Count);
            });
        }

        #endregion

    }
    public class ListViewIndexComparer : System.Collections.IComparer
    {
        public int Compare(object x, object y)
        {
            return ((ListViewItem)x).Index - ((ListViewItem)y).Index;
        }
    }

}
