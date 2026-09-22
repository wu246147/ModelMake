using HalconDotNet;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ModelMake
{
    public partial class FormModel : Form
    {
        HTuple hv_WindowHandle = null;
        public FormModel()
        {
            InitializeComponent();
            HOperatorSet.SetWindowAttr("background_color", "black");
            HOperatorSet.OpenWindow(-1, -1, hWindowControl1.Width - 4, hWindowControl1.Height - 4, hWindowControl1.HalconWindow, "", "", out hv_WindowHandle);

            comboBox过滤器.SelectedIndex = 0;
            comboBox滤波器.SelectedIndex = 0;
            comboBox非极大值抑制.SelectedIndex = 0;
        }

        List<HObject> showHObjects = new List<HObject>();
        public void DispClear()
        {
            foreach (HObject obj in showHObjects)
            {
                obj.Dispose();
            }
            showHObjects.Clear();
            HOperatorSet.ClearWindow(hv_WindowHandle);
        }
        public void DispObj(HObject hObject, bool bClone = true, bool bAdd = true)
        {
            if (hObject == null)
            {
                return;
            }
            if (bAdd) showHObjects.Add(bClone ? hObject.Clone() : hObject);
            HOperatorSet.DispObj(hObject, hv_WindowHandle);
        }
        public void DispImage(HObject image)
        {
            if (image == null)
            {
                return;
            }
            if (image is HImage)
            {
                HImage img = (HImage)image;
                int w, h;
                img.GetImageSize(out w, out h);
                HTuple row, column, width, height;
                HOperatorSet.GetWindowExtents(hv_WindowHandle, out row, out column, out width, out height);

                double wndRatio = (double)width / (double)height;
                double imgRatio = (double)w / (double)h;
                int beginRow, beginCol, endRow, endCol;
                if (wndRatio > imgRatio)
                {
                    beginRow = 0;
                    endRow = h;
                    beginCol = (int)((w - h * wndRatio) / 2d);
                    endCol = (int)(w - (w - h * wndRatio) / 2d);
                }
                else
                {
                    beginRow = (int)(-(w / wndRatio - h) / 2d);
                    endRow = (int)(h + (w / wndRatio - h) / 2d);
                    beginCol = 0;
                    endCol = w;
                }

                HOperatorSet.SetPart(hv_WindowHandle, beginRow, beginCol, endRow, endCol);
            }
            else
            {
                HOperatorSet.GetImageSize(image, out HTuple imgW, out HTuple imgH);
                double w = imgW.D, h = imgH.D;
                HOperatorSet.GetWindowExtents(hv_WindowHandle, out HTuple row, out HTuple column, out HTuple width, out HTuple height);

                double wndRatio = width.D / height.D;
                double imgRatio = w / h;
                int beginRow, beginCol, endRow, endCol;
                if (wndRatio > imgRatio)
                {
                    beginRow = 0;
                    endRow = (int)h;
                    beginCol = (int)((w - h * wndRatio) / 2d);
                    endCol = (int)(w - (w - h * wndRatio) / 2d);
                }
                else
                {
                    beginRow = (int)(-(w / wndRatio - h) / 2d);
                    endRow = (int)(h + (w / wndRatio - h) / 2d);
                    beginCol = 0;
                    endCol = (int)w;
                }

                HOperatorSet.SetPart(hv_WindowHandle, beginRow, beginCol, endRow, endCol);
            }
            foreach (HObject obj in showHObjects)
            {
                obj.Dispose();
            }
            showHObjects.Clear();
            showHObjects.Add(image.Clone());
            HOperatorSet.DispObj(image, hv_WindowHandle);
        }
        HObject PaintRegionRed(HObject image, HObject region)
        {
            HObject or, og, ob;
            HOperatorSet.CountChannels(image, out HTuple num);
            if (num == 3)
            {
                HOperatorSet.Decompose3(image, out or, out og, out ob);
            }
            else
            {
                HOperatorSet.Rgb1ToGray(image, out HObject grayImage);
                or = grayImage;
                og = grayImage;
                ob = grayImage;
            }
            HOperatorSet.GetImageSize(image, out HTuple width, out HTuple height);
            HOperatorSet.GenRectangle1(out HObject allRegion, 0, 0, height, width);
            HOperatorSet.Difference(allRegion, region, out HObject regionDiff);
            allRegion.Dispose();

            HOperatorSet.PaintRegion(regionDiff, or, out HObject tr, 255, "fill");
            HOperatorSet.AddImage(or, tr, out HObject nr, 0.5, 0);

            HOperatorSet.PaintRegion(regionDiff, og, out HObject tg, 0, "fill");
            HOperatorSet.AddImage(og, tg, out HObject ng, 0.5, 0);

            HOperatorSet.PaintRegion(regionDiff, ob, out HObject tb, 0, "fill");
            HOperatorSet.AddImage(ob, tb, out HObject nb, 0.5, 0);

            HOperatorSet.Compose3(nr, ng, nb, out HObject rgb);
            regionDiff.Dispose();
            or.Dispose();
            tr.Dispose();
            nr.Dispose();
            og.Dispose();
            tg.Dispose();
            ng.Dispose();
            ob.Dispose();
            tb.Dispose();
            nb.Dispose();

            return rgb;
        }

        HObject image = null;
        string[] filePaths = null;
        private void label_Load_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Multiselect = true;
            ofd.Title = "请选择文件";
            ofd.Filter = "PNG|*.png|BitMap|*.bmp|HOBJ|*.hobj|所有文件|*.*";
            if (DialogResult.OK == ofd.ShowDialog())
            {
                filePaths = ofd.FileNames;
                comboBox_source.Items.Clear();
                comboBox_source.Items.AddRange(filePaths);
                comboBox_source.SelectedIndex = 0;
                label_index.Text = comboBox_source.SelectedIndex.ToString();
                LoadHImage();
            }
        }

        private void LoadHImage()
        {
            label分数.Text = "----";
            labelSave.ForeColor = Color.Gray;
            image?.Dispose();
            image = null;
            try
            {
                HOperatorSet.ReadImage(out image, comboBox_source.Text);
            }
            catch (Exception ex)
            {
                MessageBox.Show("图片格式不对:" + ex.Message);
                DispClear();
                return;
            }

            DispImage(image);
            保存路径解析();
        }

        private void 保存路径解析()
        {
            try
            {
                if (comboBox_source.Text != string.Empty)
                {
                    string dir = Path.GetDirectoryName(comboBox_source.Text);
                    string fileName = Path.GetFileNameWithoutExtension(comboBox_source.Text);
                    string[] strings = fileName.Split('_');
                    if (strings.Length >= 2)
                    {
                        string camName = strings[0];
                        //string poseNum = strings[1];
                        string poseNum = fileName.Remove(0, strings[0].Length+1);
                        string modExtension = radioButton_shm.Checked ? "shm" : "ncc";
                        //if (checkBox_区分相机.Checked)
                        //{
                        textBox_模板保存路径.Text = $"{dir}\\{camName}\\Model\\{poseNum}.{modExtension}";
                        textBox_txt保存路径.Text = $"{dir}\\{camName}\\Model\\{poseNum}.txt";
                        //}
                        //else
                        //{
                            //textBox_模板保存路径.Text = $"{dir}\\Model\\{poseNum}.{modExtension}";
                            //textBox_txt保存路径.Text = $"{dir}\\Model\\{poseNum}.txt";
                        //}
                        textBox_模板保存路径.SelectionStart = textBox_模板保存路径.TextLength;
                        textBox_txt保存路径.SelectionStart = textBox_txt保存路径.TextLength;
                    }
                    else
                    {
                        textBox_模板保存路径.Text = string.Empty;
                        textBox_txt保存路径.Text = string.Empty;
                        MessageBox.Show("图片名称格式不对（相机名_点位号）");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void checkBox_区分相机_CheckedChanged(object sender, EventArgs e)
        {
            保存路径解析();
        }

        private void radioButton_shm_CheckedChanged(object sender, EventArgs e)
        {
            保存路径解析();
            groupBox4.Enabled = radioButton_shm.Checked;
            checkBox_Scale.Enabled = radioButton_shm.Checked;
        }

        private void comboBox_source_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadHImage();
        }
        private void label_index_Click(object sender, EventArgs e)
        {
            LoadHImage();
        }
        private void label_up_Click(object sender, EventArgs e)
        {
            if (comboBox_source.Items.Count < 1)
            {
                return;
            }
            if (comboBox_source.SelectedIndex <= 0)
            {
                comboBox_source.SelectedIndex = comboBox_source.Items.Count - 1;
            }
            else
            {
                comboBox_source.SelectedIndex -= 1;
            }
            label_index.Text = comboBox_source.SelectedIndex.ToString();
        }
        private void label_down_Click(object sender, EventArgs e)
        {
            if (comboBox_source.Items.Count < 1)
            {
                return;
            }
            if (comboBox_source.SelectedIndex >= comboBox_source.Items.Count - 1)
            {
                comboBox_source.SelectedIndex = 0;
            }
            else
            {
                comboBox_source.SelectedIndex += 1;
            }
            label_index.Text = comboBox_source.SelectedIndex.ToString();
        }

        bool drawing = false;
        HObject HRegion搜索 = new HObject();
        private void button绘制搜索区域_Click(object sender, EventArgs e)
        {
            try
            {
                if (drawing)
                {
                    return;
                }
                //提示
                DispClear();
                DispObj(image);
                HOperatorSet.GetPart(hv_WindowHandle, out HTuple row1, out HTuple column1, out HTuple row2, out HTuple column2);
                HOperatorSet.SetColor(hv_WindowHandle, "red");
                HOperatorSet.SetTposition(hv_WindowHandle, row1 + 1, column1 + 10);
                HOperatorSet.WriteString(hv_WindowHandle, "左键:编辑 右键:完成绘制");

                HTuple hv_Row1 = null, hv_Column1 = null, hv_Row2 = null, hv_Column2 = null;
                hWindowControl1.Select();
                HOperatorSet.SetColor(hv_WindowHandle, "red");
                drawing = true;
                if (numericUpDownRow1.Value != 0 || numericUpDownRow2.Value != 0 || numericUpDownColumn1.Value != 0 || numericUpDownColumn2.Value != 0)
                {
                    HOperatorSet.DrawRectangle1Mod(hv_WindowHandle, (int)numericUpDownRow1.Value, (int)numericUpDownColumn1.Value, (int)numericUpDownRow2.Value, (int)numericUpDownColumn2.Value, out hv_Row1, out hv_Column1, out hv_Row2, out hv_Column2);
                }
                else
                {
                    HOperatorSet.DrawRectangle1(hv_WindowHandle, out hv_Row1, out hv_Column1, out hv_Row2, out hv_Column2);
                }
                drawing = false;
                numericUpDownRow1.Value = (int)hv_Row1.D;
                numericUpDownColumn1.Value = (int)hv_Column1.D;
                numericUpDownRow2.Value = (int)hv_Row2.D;
                numericUpDownColumn2.Value = (int)hv_Column2.D;

                HRegion搜索.Dispose();
                HOperatorSet.GenRectangle1(out HRegion搜索, (int)numericUpDownRow1.Value, (int)numericUpDownColumn1.Value, (int)numericUpDownRow2.Value, (int)numericUpDownColumn2.Value);
                HRegion.Dispose();
                HRegion = HRegion搜索.Clone();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        HObject HRegion = new HObject();//有效区域
        private void button遮罩_Click(object sender, EventArgs e)
        {
            try
            {
                if (drawing)
                {
                    return;
                }

                //提示
                DispClear();
                DispObj(PaintRegionRed(image, HRegion), false);
                HOperatorSet.GetPart(hv_WindowHandle, out HTuple row1, out HTuple column1, out HTuple row2, out HTuple column2);
                HOperatorSet.SetColor(hv_WindowHandle, "red");
                HOperatorSet.SetTposition(hv_WindowHandle, row1 + 1, column1 + 10);
                HOperatorSet.WriteString(hv_WindowHandle, "左键:编辑 右键:完成绘制");
                hWindowControl1.Select();
                HOperatorSet.SetColor(hv_WindowHandle, "red");

                drawing = true;
                HObject hObject;
                if (radioButton正交矩形.Checked)
                {
                    HOperatorSet.DrawRectangle1(hv_WindowHandle, out HTuple row11, out HTuple column11, out HTuple row22, out HTuple column22);
                    HOperatorSet.GenRectangle1(out hObject, row11, column11, row22, column22);
                }
                else if (radioButton旋转矩形.Checked)
                {
                    HOperatorSet.DrawRectangle2(hv_WindowHandle, out HTuple row, out HTuple column, out HTuple phi, out HTuple length1, out HTuple length2);
                    HOperatorSet.GenRectangle2(out hObject, row, column, phi, length1, length2);
                }
                else if (radioButton圆形.Checked)
                {
                    HOperatorSet.DrawCircle(hv_WindowHandle, out HTuple row, out HTuple column, out HTuple radius);
                    HOperatorSet.GenCircle(out hObject, row, column, radius);
                }
                else if (radioButton椭圆形.Checked)
                {
                    HOperatorSet.DrawEllipse(hv_WindowHandle, out HTuple row, out HTuple column, out HTuple phi, out HTuple radius1, out HTuple radius2);
                    HOperatorSet.GenEllipse(out hObject, row, column, phi, radius1, radius2);
                }
                else
                {
                    //正交矩形
                    HOperatorSet.DrawRectangle1(hv_WindowHandle, out HTuple row11, out HTuple column11, out HTuple row22, out HTuple column22);
                    HOperatorSet.GenRectangle1(out hObject, row11, column11, row22, column22);
                }
                drawing = false;

                HOperatorSet.Difference(HRegion, hObject, out HObject HRegionDiff);
                hObject.Dispose();
                HRegion.Dispose();
                HRegion = HRegionDiff;
                DispClear();
                DispObj(PaintRegionRed(image, HRegion), false);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private void button橡皮擦_Click(object sender, EventArgs e)
        {
            try
            {
                if (drawing)
                {
                    return;
                }
                //提示
                DispClear();
                DispObj(PaintRegionRed(image, HRegion), false);
                HOperatorSet.GetPart(hv_WindowHandle, out HTuple row1, out HTuple column1, out HTuple row2, out HTuple column2);
                HOperatorSet.SetColor(hv_WindowHandle, "red");
                HOperatorSet.SetTposition(hv_WindowHandle, row1 + 1, column1 + 10);
                HOperatorSet.WriteString(hv_WindowHandle, "左键:编辑 右键:完成绘制");
                hWindowControl1.Select();
                HOperatorSet.SetColor(hv_WindowHandle, "red");

                drawing = true;
                HObject hObject;
                if (radioButton正交矩形.Checked)
                {
                    HOperatorSet.DrawRectangle1(hv_WindowHandle, out HTuple row11, out HTuple column11, out HTuple row22, out HTuple column22);
                    HOperatorSet.GenRectangle1(out hObject, row11, column11, row22, column22);
                }
                else if (radioButton旋转矩形.Checked)
                {
                    HOperatorSet.DrawRectangle2(hv_WindowHandle, out HTuple row, out HTuple column, out HTuple phi, out HTuple length1, out HTuple length2);
                    HOperatorSet.GenRectangle2(out hObject, row, column, phi, length1, length2);
                }
                else if (radioButton圆形.Checked)
                {
                    HOperatorSet.DrawCircle(hv_WindowHandle, out HTuple row, out HTuple column, out HTuple radius);
                    HOperatorSet.GenCircle(out hObject, row, column, radius);
                }
                else if (radioButton椭圆形.Checked)
                {
                    HOperatorSet.DrawEllipse(hv_WindowHandle, out HTuple row, out HTuple column, out HTuple phi, out HTuple radius1, out HTuple radius2);
                    HOperatorSet.GenEllipse(out hObject, row, column, phi, radius1, radius2);
                }
                else
                {
                    //正交矩形
                    HOperatorSet.DrawRectangle1(hv_WindowHandle, out HTuple row11, out HTuple column11, out HTuple row22, out HTuple column22);
                    HOperatorSet.GenRectangle1(out hObject, row11, column11, row22, column22);
                }
                drawing = false;

                HOperatorSet.Union2(HRegion, hObject, out HObject HRegionUnion);
                hObject.Dispose();
                HRegion.Dispose();
                HRegion = HRegionUnion;
                DispClear();
                DispObj(PaintRegionRed(image, HRegion), false);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void button图像涂黑_Click(object sender, EventArgs e)
        {
            try
            {
                if (drawing)
                {
                    return;
                }
                //提示
                DispClear();
                DispObj(image);
                DispObj(modXld);
                HOperatorSet.GetPart(hv_WindowHandle, out HTuple row1, out HTuple column1, out HTuple row2, out HTuple column2);
                HOperatorSet.SetColor(hv_WindowHandle, "red");
                HOperatorSet.SetTposition(hv_WindowHandle, row1 + 1, column1 + 10);
                HOperatorSet.WriteString(hv_WindowHandle, "左键:编辑 右键:完成绘制");
                hWindowControl1.Select();
                HOperatorSet.SetColor(hv_WindowHandle, "red");

                drawing = true;
                HObject hObject;
                if (radioButton正交矩形.Checked)
                {
                    HOperatorSet.DrawRectangle1(hv_WindowHandle, out HTuple row11, out HTuple column11, out HTuple row22, out HTuple column22);
                    HOperatorSet.GenRectangle1(out hObject, row11, column11, row22, column22);
                }
                else if (radioButton旋转矩形.Checked)
                {
                    HOperatorSet.DrawRectangle2(hv_WindowHandle, out HTuple row, out HTuple column, out HTuple phi, out HTuple length1, out HTuple length2);
                    HOperatorSet.GenRectangle2(out hObject, row, column, phi, length1, length2);
                }
                else if (radioButton圆形.Checked)
                {
                    HOperatorSet.DrawCircle(hv_WindowHandle, out HTuple row, out HTuple column, out HTuple radius);
                    HOperatorSet.GenCircle(out hObject, row, column, radius);
                }
                else if (radioButton椭圆形.Checked)
                {
                    HOperatorSet.DrawEllipse(hv_WindowHandle, out HTuple row, out HTuple column, out HTuple phi, out HTuple radius1, out HTuple radius2);
                    HOperatorSet.GenEllipse(out hObject, row, column, phi, radius1, radius2);
                }
                else
                {
                    //正交矩形
                    HOperatorSet.DrawRectangle1(hv_WindowHandle, out HTuple row11, out HTuple column11, out HTuple row22, out HTuple column22);
                    HOperatorSet.GenRectangle1(out hObject, row11, column11, row22, column22);
                }
                drawing = false;

                HOperatorSet.PaintRegion(hObject, image, out HObject imagePaint, 0, "fill");
                hObject.Dispose();
                image.Dispose();
                image = imagePaint;
                DispClear();
                DispObj(image);
                DispObj(modXld);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void button图像涂白_Click(object sender, EventArgs e)
        {
            try
            {
                if (drawing)
                {
                    return;
                }
                //提示
                DispClear();
                DispObj(image);
                DispObj(modXld);
                HOperatorSet.GetPart(hv_WindowHandle, out HTuple row1, out HTuple column1, out HTuple row2, out HTuple column2);
                HOperatorSet.SetColor(hv_WindowHandle, "red");
                HOperatorSet.SetTposition(hv_WindowHandle, row1 + 1, column1 + 10);
                HOperatorSet.WriteString(hv_WindowHandle, "左键:编辑 右键:完成绘制");
                hWindowControl1.Select();
                HOperatorSet.SetColor(hv_WindowHandle, "red");

                drawing = true;
                HObject hObject;
                if (radioButton正交矩形.Checked)
                {
                    HOperatorSet.DrawRectangle1(hv_WindowHandle, out HTuple row11, out HTuple column11, out HTuple row22, out HTuple column22);
                    HOperatorSet.GenRectangle1(out hObject, row11, column11, row22, column22);
                }
                else if (radioButton旋转矩形.Checked)
                {
                    HOperatorSet.DrawRectangle2(hv_WindowHandle, out HTuple row, out HTuple column, out HTuple phi, out HTuple length1, out HTuple length2);
                    HOperatorSet.GenRectangle2(out hObject, row, column, phi, length1, length2);
                }
                else if (radioButton圆形.Checked)
                {
                    HOperatorSet.DrawCircle(hv_WindowHandle, out HTuple row, out HTuple column, out HTuple radius);
                    HOperatorSet.GenCircle(out hObject, row, column, radius);
                }
                else if (radioButton椭圆形.Checked)
                {
                    HOperatorSet.DrawEllipse(hv_WindowHandle, out HTuple row, out HTuple column, out HTuple phi, out HTuple radius1, out HTuple radius2);
                    HOperatorSet.GenEllipse(out hObject, row, column, phi, radius1, radius2);
                }
                else
                {
                    //正交矩形
                    HOperatorSet.DrawRectangle1(hv_WindowHandle, out HTuple row11, out HTuple column11, out HTuple row22, out HTuple column22);
                    HOperatorSet.GenRectangle1(out hObject, row11, column11, row22, column22);
                }
                drawing = false;

                HOperatorSet.PaintRegion(hObject, image, out HObject imagePaint, 255, "fill");
                hObject.Dispose();
                image.Dispose();
                image = imagePaint;
                DispClear();
                DispObj(image);
                DispObj(modXld);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        HObject modXld = null;
        private void button提取轮廓_Click(object sender, EventArgs e)
        {
            try
            {
                HObject reg;
                if (radioButton_sobel.Checked)
                {
                    //****************方法1：sobel_amp计算图像的一阶导数，用作边缘检测器*******************
                    string 过滤器 = comboBox过滤器.Text;//List of values(for compute devices): 'sum_abs', 'sum_sqrt', 'x', 'y', 'sum_abs_binomial', 'sum_sqrt_binomial', 'x_binomial', 'y_binomial'   
                    int 掩膜尺寸 = (int)numericUpDown掩膜尺寸.Value;//List of values: 3, 5, 7, 9, 11, 13, 15, 17, 19, 21, 23, 25, 27, 29, 31, 33, 35, 37, 39

                    HOperatorSet.SobelAmp(image, out HObject imageAmp, 过滤器, 掩膜尺寸);
                    int 边缘阈值 = (int)numericUpDown边缘阈值.Value;
                    HOperatorSet.Threshold(imageAmp, out reg, 边缘阈值, 255);
                    imageAmp.Dispose();
                }
                else
                {
                    //****************方法2：edges_image使用Deriche,Lanser,Shen或Canny过滤器提取边缘*******************
                    string 滤波器 = comboBox滤波器.Text;//List of values (for compute devices): 'canny', 'sobel_fast'
                    double 平滑系数 = (double)numericUpDown平滑系数.Value;//0.2 ≤ Alpha ≤ 50.0
                    string 非极大值抑制 = comboBox非极大值抑制.Text;//List of values: 'hvnms', 'inms', 'nms', 'none'
                    double 滞后阈值下限 = (double)numericUpDown滞后阈值下限.Value;
                    double 滞后阈值上限 = (double)numericUpDown滞后阈值上限.Value;
                    HOperatorSet.EdgesImage(image, out HObject imageAmp, out HObject imaDir, 滤波器, 平滑系数, 非极大值抑制, 滞后阈值下限, 滞后阈值上限);
                    HOperatorSet.Threshold(imageAmp, out reg, 滞后阈值下限, 255);
                    imageAmp.Dispose();
                    imaDir.Dispose();
                }

                HOperatorSet.Intersection(HRegion, reg, out HObject contsReg);//交集
                reg.Dispose();
                HOperatorSet.Skeleton(contsReg, out HObject skeletonReg);//抽骨架
                contsReg.Dispose();
                HOperatorSet.GenContourRegionXld(skeletonReg, out HObject contsXld, "center");//转轮廓
                skeletonReg.Dispose();

                HOperatorSet.AreaCenterPointsXld(contsXld, out HTuple area, out HTuple r, out HTuple c);
                //排序
                int[] indexs = BubbleSortIndex降序(area.ToDArr());
                HObject selectXlds = null;
                int 实际轮廓数量 = Math.Min((int)numericUpDown轮廓数量.Value, indexs.Length);
                for (int i = 0; i < 实际轮廓数量; i++)
                {
                    HOperatorSet.SelectObj(contsXld, out HObject selectXld, indexs[i] + 1);

                    if (selectXlds == null)
                    {
                        selectXlds = selectXld;
                    }
                    else
                    {
                        HOperatorSet.ConcatObj(selectXlds, selectXld, out HObject concatXld);
                        selectXlds.Dispose();
                        selectXlds = concatXld;
                    }
                }
                contsXld.Dispose();
                if (selectXlds != null)
                {
                    int numRegrPionts = (int)numericUpDown轮廓平滑系数.Value;
                    if (numRegrPionts >= 3)
                    {
                        numRegrPionts = numRegrPionts + numRegrPionts % 2 - 1;
                        HOperatorSet.SmoothContoursXld(selectXlds, out HObject SmoothXld, numRegrPionts);
                        selectXlds.Dispose();
                        selectXlds = SmoothXld;
                    }
                }

                DispClear();
                DispObj(PaintRegionRed(image, HRegion), false);
                HOperatorSet.SetColored(hv_WindowHandle, 12);
                //HOperatorSet.SetColor(hv_WindowHandle, "red");
                DispObj(selectXlds);
                modXld?.Dispose();
                modXld = selectXlds;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        /// <summary>
        /// 返回原数组降序排列后的下标（冒泡排序原理）
        /// </summary>
        /// <param name="arr"></param>
        /// <returns></returns>
        int[] BubbleSortIndex降序(double[] arr)
        {
            int[] indexs = new int[arr.Length];
            for (int i = 0; i < indexs.Length; i++)
            {
                indexs[i] = i;
            }
            for (int i = 0; i < indexs.Length - 1; i++)
            {
                for (int j = 0; j < indexs.Length - 1 - i; j++)
                {
                    if (arr[indexs[j]] < arr[indexs[j + 1]])
                    {
                        int temp = indexs[j];
                        indexs[j] = indexs[j + 1];
                        indexs[j + 1] = temp;
                    }
                }
            }
            return indexs;
        }

        HTuple modelIDxld, modelIDncc, row, column; bool making = false;
        private void button识别_Click(object sender, EventArgs e)
        {
            if (radioButton_shm.Checked)
            {
                if (modXld != null)
                {
                    if (!making)
                    {
                        making = true;
                        try
                        {
                            if (modelIDxld != null && modelIDxld.Length > 0)
                            {
                                HOperatorSet.ClearShapeModel(modelIDxld);
                                modelIDxld = null;
                            }
                            double angleMin = 0, angleLength = 0;
                            if (checkBox_Angle.Checked)
                            {
                                angleMin = (double)numericUpDown_AngleMin.Value;
                                angleLength = (double)numericUpDown_AngleMax.Value - angleMin;
                            }
                            if (checkBox_Scale.Checked)
                            {
                                HOperatorSet.CreateScaledShapeModelXld(modXld, 1, angleMin, angleLength, "auto", (double)numericUpDown_ScaleMin.Value, (double)numericUpDown_ScaleMax.Value, 0.01, "auto", "ignore_local_polarity", 5, out modelIDxld);
                            }
                            else
                            {
                                HOperatorSet.CreateShapeModelXld(modXld, 2, angleMin, angleLength, "auto", "auto", "ignore_local_polarity", 5, out modelIDxld);
                            }
                            //HOperatorSet.ReduceDomain(image, HRegion搜索, out HObject imageReduced);
                            ////中值滤波模糊噪声
                            //HOperatorSet.MedianImage(imageReduced, out HObject imageProcess, "circle",1.5,"mirrored");
                            //if (checkBox_sharpen.Checked)
                            //{
                            //    //锐化处理提高边缘对比度
                            //    HOperatorSet.Emphasize(imageProcess, out imageProcess, 15, 15, 1.5);

                            //}

                            //HOperatorSet.FindScaledShapeModel(imageProcess, modelIDxld, angleMin, angleLength, 1, 1, 0.5, 1, 0.5, "least_squares", 0, 0.9, out row, out column, out HTuple angle, out HTuple scale, out HTuple score);
                            ////HOperatorSet.FindShapeModel(imageReduced, modelID, angleMin, angleLength, 0.9, 1, 0.5, "least_squares", 0, 0.9, out row, out column, out HTuple angle, out HTuple score);
                            //if (score.Length == 0)
                            //{
                            //    throw new Exception("匹配识别失败");
                            //}
                            //label分数.Text = score.Length > 0 ? score.D.ToString("0.000") : "----";
                            labelSave.ForeColor = Color.Red;
                            //imageReduced.Dispose();
                            //imageProcess.Dispose();
                            ////imageEmphasize.Dispose();
                            ////imageEmphasize2.Dispose();

                            //HOperatorSet.GetShapeModelContours(out HObject modelContours, modelIDxld, 1);
                            //HOperatorSet.VectorAngleToRigid(0, 0, 0, row, column, angle, out HTuple homMat2D);
                            //HOperatorSet.HomMat2dScale(homMat2D, scale, scale, row, column, out homMat2D);
                            //HOperatorSet.AffineTransContourXld(modelContours, out HObject xldAff, homMat2D);
                            //modelContours.Dispose();
                            //DispClear();
                            //DispObj(image);
                            //DispObj(xldAff, false);
                            //HOperatorSet.GenCrossContourXld(out HObject cross, row, column, 7, angle);
                            //DispObj(cross, false);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.ToString());
                        }
                        making = false;
                    }
                }
            }
            else if (radioButton_ncc.Checked)
            {
                if (!making)
                {
                    making = true;
                    try
                    {
                        if (modelIDncc != null && modelIDncc.Length > 0)
                        {
                            HOperatorSet.ClearNccModel(modelIDncc);
                            modelIDncc = null;
                        }
                        double angleMin = 0, angleLength = 0;
                        if (checkBox_Angle.Checked)
                        {
                            angleMin = (double)numericUpDown_AngleMin.Value;
                            angleLength = (double)numericUpDown_AngleMax.Value - angleMin;
                        }

                        HOperatorSet.ReduceDomain(image, HRegion, out HObject imageMod);
                        HOperatorSet.CreateNccModel(imageMod, 1, angleMin, angleLength, "auto", "use_polarity", out modelIDncc);

                        //HOperatorSet.ReduceDomain(image, HRegion搜索, out HObject imageReduced);
                        //HOperatorSet.FindNccModel(imageReduced, modelIDncc, angleMin, angleLength, 0.7, 1, 0.5, "true", 0, out row, out column, out HTuple angle, out HTuple score);
                        //HOperatorSet.GenCrossContourXld(out HObject cross, row, column, 57, angle);
                        //label分数.Text = score.Length > 0 ? score.D.ToString("0.000") : "----";
                        labelSave.ForeColor = Color.Red;
                        //imageReduced.Dispose();
                        //DispClear();
                        //DispObj(image);
                        //DispObj(cross, false);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.ToString());
                    }
                    making = false;
                }
            }
        }

        private void hWindowControl1_SizeChanged(object sender, EventArgs e)
        {
            HOperatorSet.SetWindowExtents(hv_WindowHandle, -1, -1, hWindowControl1.Width - 4, hWindowControl1.Height - 4);
            foreach (var item in showHObjects)
            {
                DispObj(item, false, false);
            }
        }

        private void button拟合直线_Click(object sender, EventArgs e)
        {
            //if (modXld != null)
            //{
            //    try
            //    {
            //        //打断
            //        HOperatorSet.SegmentContoursXld(modXld, out HObject modXldSplit, "lines_circles", 5, 4, 2);
            //        HOperatorSet.SelectContoursXld(modXldSplit, out HObject selectXld, "contour_length", 0.5, 20000, -0.5, 0.5);
            //        //HOperatorSet.SelectObj(modXld, out HObject selectXld, 1);
            //        DispClear();
            //        DispObj(PaintRegionRed(image, HRegion), false);
            //        HOperatorSet.SetColored(hv_WindowHandle, 12);
            //        DispObj(selectXld);
            //        //连接
            //        HOperatorSet.UnionAdjacentContoursXld(selectXld, out HObject xld1, 10, 1, "attr_keep");
            //        HOperatorSet.UnionCollinearContoursXld(xld1, out HObject xld2, 10, 1, 2, 0.1, "attr_keep");
            //        HOperatorSet.UnionCocircularContoursXld(xld2, out HObject xld3, 0.5, 0.1, 0.2, 30, 10, 10, "true", 1);
            //        DispClear();
            //        DispObj(PaintRegionRed(image, HRegion), false);
            //        HOperatorSet.SetColored(hv_WindowHandle, 12);
            //        DispObj(xld3);
            //        ////拟合

            //        //HOperatorSet.FitCircleContourXld(selectXld, "ahuber", -1, 0, 0, 3, 2, out HTuple row, out HTuple column, out HTuple radius, out HTuple startPhi, out HTuple endPhi, out HTuple pointOrder);
            //        //HOperatorSet.GenCircleContourXld(out HObject contCircle, row, column, radius, startPhi, endPhi, pointOrder, 1.0);
            //        //DispClear();
            //        //DispObj(PaintRegionRed(image, HRegion), false);
            //        //DispObj(contCircle);
            //        ////连接

            //    }
            //    catch (Exception ex)
            //    {
            //        MessageBox.Show(ex.ToString());
            //    }
            //}
        }

        private void button拟合圆弧_Click(object sender, EventArgs e)
        {
            if (modXld != null)
            {
                try
                {
                    HOperatorSet.FitCircleContourXld(modXld, "atukey", -1, 0, 10, 3, 20, out HTuple row, out HTuple column, out HTuple radius, out HTuple startPhi, out HTuple endPhi, out HTuple pointOrder);
                    HOperatorSet.GenCircleContourXld(out HObject cont, row, column, radius, startPhi, endPhi, pointOrder, 1.0);
                    modXld.Dispose();
                    modXld = cont;
                    DispClear();
                    DispObj(PaintRegionRed(image, HRegion), false);
                    DispObj(cont);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            }
        }

        private void button拟合椭圆_Click(object sender, EventArgs e)
        {
            if (modXld != null)
            {
                try
                {
                    HOperatorSet.FitEllipseContourXld(modXld, "fitzgibbon", -1, 2, 0, 200, 3, 2, out HTuple row, out HTuple column, out HTuple phi, out HTuple radius1, out HTuple radius2, out HTuple startPhi, out HTuple endPhi, out HTuple pointOrder);
                    HOperatorSet.GenEllipseContourXld(out HObject cont, row, column, phi, radius1, radius2, startPhi, endPhi, pointOrder, 1.0);
                    modXld.Dispose();
                    modXld = cont;
                    DispClear();
                    DispObj(PaintRegionRed(image, HRegion), false);
                    DispObj(cont);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            }
        }

        private void checkBox_Angle_CheckedChanged(object sender, EventArgs e)
        {
            numericUpDown_AngleMin.Enabled = checkBox_Angle.Checked;
            numericUpDown_AngleMax.Enabled = checkBox_Angle.Checked;
        }

        private void checkBox_Scale_CheckedChanged(object sender, EventArgs e)
        {
            numericUpDown_ScaleMin.Enabled = checkBox_Scale.Checked;
            numericUpDown_ScaleMax.Enabled = checkBox_Scale.Checked;
        }

        private void buttonSave_Click(object sender, EventArgs e)
        {
            if (radioButton_shm.Checked && modelIDxld == null || radioButton_ncc.Checked && modelIDncc == null)
            {
                MessageBox.Show("保存失败：未制作模板");
                return;
            }
            if (textBox_模板保存路径.Text == string.Empty)
            {
                MessageBox.Show("保存失败：shm保存路径为空");
                return;
            }
            if (textBox_txt保存路径.Text == string.Empty)
            {
                MessageBox.Show("保存失败：txt保存路径为空");
                return;
            }

            try
            {
                string dir = Path.GetDirectoryName(textBox_模板保存路径.Text);
                Directory.CreateDirectory(dir);
                if (radioButton_shm.Checked)
                {
                    HOperatorSet.WriteShapeModel(modelIDxld, textBox_模板保存路径.Text);
                }
                else
                {
                    HOperatorSet.WriteNccModel(modelIDncc, textBox_模板保存路径.Text);
                }
                using (StreamWriter writer = new StreamWriter(textBox_txt保存路径.Text, false, Encoding.UTF8))
                {
                    int row1 = (int)numericUpDownRow1.Value;
                    int column1 = (int)numericUpDownColumn1.Value;
                    int row2 = (int)numericUpDownRow2.Value;
                    int column2 = (int)numericUpDownColumn2.Value;
                    writer.WriteLine($"{row1},{column1},{row2},{column2}");
                    writer.WriteLine($"{row.D:0.0},{column.D:0.0}");
                    writer.Close();
                }
                labelSave.ForeColor = Color.Green;
                MessageBox.Show("保存成功！");
            }
            catch (Exception ex)
            {
                MessageBox.Show("保存失败：" + ex.ToString());
            }
        }

        private void button_check_Click(object sender, EventArgs e)
        {
            if (radioButton_shm.Checked)
            {
                if (modXld != null)
                {
                    if (!making)
                    {
                        making = true;
                        try
                        {
                            if (modelIDxld != null && modelIDxld.Length > 0)
                            {
                                double angleMin = 0, angleLength = 0;
                            if (checkBox_Angle.Checked)
                            {
                                angleMin = (double)numericUpDown_AngleMin.Value;
                                angleLength = (double)numericUpDown_AngleMax.Value - angleMin;
                            }
                           
                            HOperatorSet.ReduceDomain(image, HRegion搜索, out HObject imageReduced);
                            //中值滤波模糊噪声
                            HOperatorSet.MedianImage(imageReduced, out HObject imageProcess, "circle", 1.5, "mirrored");
                            if (checkBox_sharpen.Checked)
                            {
                                //锐化处理提高边缘对比度
                                HOperatorSet.Emphasize(imageProcess, out imageProcess, 15, 15, 1.5);

                            }

                            HOperatorSet.FindScaledShapeModel(imageProcess, modelIDxld, angleMin, angleLength, 1, 1, 0.5, 1, 0.5, "least_squares", 0, 0.9, out row, out column, out HTuple angle, out HTuple scale, out HTuple score);
                            //HOperatorSet.FindShapeModel(imageReduced, modelID, angleMin, angleLength, 0.9, 1, 0.5, "least_squares", 0, 0.9, out row, out column, out HTuple angle, out HTuple score);
                            if (score.Length == 0)
                            {
                                throw new Exception("匹配识别失败");
                            }
                            label分数.Text = score.Length > 0 ? score.D.ToString("0.000") : "----";
                            //labelSave.ForeColor = Color.Red;
                            imageReduced.Dispose();
                            imageProcess.Dispose();
                            //imageEmphasize.Dispose();
                            //imageEmphasize2.Dispose();

                            HOperatorSet.GetShapeModelContours(out HObject modelContours, modelIDxld, 1);
                            HOperatorSet.VectorAngleToRigid(0, 0, 0, row, column, angle, out HTuple homMat2D);
                            HOperatorSet.HomMat2dScale(homMat2D, scale, scale, row, column, out homMat2D);
                            HOperatorSet.AffineTransContourXld(modelContours, out HObject xldAff, homMat2D);
                            modelContours.Dispose();
                            DispClear();
                            DispObj(image);
                            DispObj(xldAff, false);
                            HOperatorSet.GenCrossContourXld(out HObject cross, row, column, 7, angle);
                            DispObj(cross, false);
                            }
                            
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.ToString());
                        }
                        making = false;
                    }
                }
            }
            else if (radioButton_ncc.Checked)
            {
                if (!making)
                {
                    making = true;
                    try
                    {
                        if (modelIDncc != null && modelIDncc.Length > 0)
                        {
                            double angleMin = 0, angleLength = 0;
                            if (checkBox_Angle.Checked)
                            {
                                angleMin = (double)numericUpDown_AngleMin.Value;
                                angleLength = (double)numericUpDown_AngleMax.Value - angleMin;
                            }


                            HOperatorSet.ReduceDomain(image, HRegion搜索, out HObject imageReduced);
                            HOperatorSet.FindNccModel(imageReduced, modelIDncc, angleMin, angleLength, 0.7, 1, 0.5, "true", 0, out row, out column, out HTuple angle, out HTuple score);
                            HOperatorSet.GenCrossContourXld(out HObject cross, row, column, 57, angle);
                            label分数.Text = score.Length > 0 ? score.D.ToString("0.000") : "----";
                            //labelSave.ForeColor = Color.Red;
                            imageReduced.Dispose();
                            DispClear();
                            DispObj(image);
                            DispObj(cross, false);
                        }
                       
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.ToString());
                    }
                    making = false;
                }
            }

        }

        private void FormModel_Paint(object sender, PaintEventArgs e)
        {
            Control control = (Control)sender;
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;//抗锯齿

            GraphicsPath graphicsPath = new GraphicsPath();
            if (control.ClientRectangle.Width > 0 && control.ClientRectangle.Height > 0)
            {
                graphicsPath.AddRectangle(control.ClientRectangle);
                LinearGradientBrush brush = new LinearGradientBrush(control.ClientRectangle, Color.FromArgb(100, 212, 225), Color.FromArgb(100, 162, 225), LinearGradientMode.BackwardDiagonal);
                g.FillPath(brush, graphicsPath);
            }
        }

        /// <summary>
        /// 滚动滑轮，缩放图像
        /// </summary>
        private void hWindowControl1_HMouseWheel(object sender, HMouseEventArgs e)
        {
            try
            {
                if (image != null && image.IsInitialized())
                {
                    HOperatorSet.GetMpositionSubPix(hv_WindowHandle, out HTuple r, out HTuple c, out HTuple button);
                    double mposRow = r.D, mposCol = c.D;

                    HOperatorSet.GetPart(hv_WindowHandle, out HTuple r1, out HTuple c1, out HTuple r2, out HTuple c2);
                    int oldBeginRow = r1, oldBeginCol = c1, oldEndRow = r2, oldEndCol = c2;

                    int zoomBeginRow = 0, zoomBeginCol = 0, zoomEndRow = 0, zoomEndCol = 0;
                    if (e.Delta > 0)
                    {
                        zoomBeginRow = (int)(oldBeginRow + (mposRow - oldBeginRow) * 0.2d);
                        zoomBeginCol = (int)(oldBeginCol + (mposCol - oldBeginCol) * 0.2d);
                        zoomEndRow = (int)(oldEndRow - (oldEndRow - mposRow) * 0.2d);
                        zoomEndCol = (int)(oldEndCol - (oldEndCol - mposCol) * 0.2d);
                    }
                    else
                    {
                        zoomBeginRow = (int)(mposRow - (mposRow - oldBeginRow) / 0.8d);
                        zoomBeginCol = (int)(mposCol - (mposCol - oldBeginCol) / 0.8d);
                        zoomEndRow = (int)(mposRow + (oldEndRow - mposRow) / 0.8d);
                        zoomEndCol = (int)(mposCol + (oldEndCol - mposCol) / 0.8d);
                    }

                    HOperatorSet.GetImageSize(image, out HTuple imgW, out HTuple imgH);
                    int imgWidth = imgW, imgHeight = imgH;

                    bool outOfArea = zoomBeginRow >= imgHeight || zoomEndRow <= 0 || zoomBeginCol >= imgWidth || zoomEndCol <= 0;
                    bool outOfSize = (zoomEndRow - zoomBeginRow) > imgWidth * 20 || (zoomEndCol - zoomBeginCol) > imgWidth * 20;
                    bool outOfPixel = (hWindowControl1.Height / (zoomEndRow - zoomBeginRow) > 500)
                        || (hWindowControl1.Width / (zoomEndCol - zoomBeginCol)) > 500;

                    if (outOfArea || outOfSize)
                    {

                    }
                    else if (!outOfPixel)
                    {
                        HOperatorSet.ClearWindow(hv_WindowHandle);
                        HOperatorSet.SetPaint(hv_WindowHandle, new HTuple("default"));
                        HOperatorSet.SetPart(hv_WindowHandle, zoomBeginRow, zoomBeginCol, zoomEndRow, zoomBeginCol + (zoomEndRow - zoomBeginRow) * hWindowControl1.Width / hWindowControl1.Height);
                        foreach (var item in showHObjects)
                        {
                            DispObj(item, false, false);
                        }

                    }
                }
            }
            catch (Exception)
            {

            }
        }

        private void radioButton_sobel_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton_sobel.Checked)
            {
                groupBox_edges.Enabled = false;
                groupBox_sobel.Enabled = true;
            }
            else
            {
                groupBox_sobel.Enabled = false;
                groupBox_edges.Enabled = true;
            }
        }

    }
}
