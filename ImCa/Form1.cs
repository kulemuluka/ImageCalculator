using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using OpenCvSharp;
using Patagames.Ocr;

namespace ImCa
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            MyConnection = new SqlConnection(connection);
            RefreshHistory();
        }

        private void RefreshHistory()
        {
            comboBox1.Items.Clear();
            SqlDataReader Reader1;
            string ComDel = "Select expression from loghistory where login = @login";
            SqlCommand cmd1 = new SqlCommand(ComDel, MyConnection);
            SqlParameter pr1 = new SqlParameter("@login", textBox2.Text);
            cmd1.Parameters.Add(pr1);

            MyConnection.Open();
            Reader1 = cmd1.ExecuteReader();
            while (Reader1.Read())
            {
                comboBox1.Items.Add(Reader1[0].ToString());
            }
            MyConnection.Close();
        }

        string text = "";
        string connection = @"Data Source=LAPTOP-CEC8DG0R;Initial Catalog=history;Integrated Security=True";
        SqlConnection MyConnection;

        private void button1_Click(object sender, EventArgs e)
        {
            label1.Visible = false;
            label3.Text = "";
            openFileDialog1.ShowDialog();
            string file = openFileDialog1.FileName;

            try
            {
                pictureBox1.ImageLocation = file;

                Mat image = new Mat(file);
                Mat grayImage = new Mat();
                Cv2.DetailEnhance(image, grayImage);
                Cv2.BilateralFilter(image, grayImage, 3, 75, 75);
                Cv2.CvtColor(image, grayImage, ColorConversionCodes.BGR2GRAY);                
                grayImage.SaveImage("im.jpg");

                using (var objOcr = OcrApi.Create())
                {
                    objOcr.Init(Patagames.Ocr.Enums.Languages.English);
                    text = objOcr.GetTextFromImage("im.jpg");
                    textBox1.Text = text;
                }
            }
            catch
            {
                label3.Text = "Выбран неверный файл!";
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            label1.Visible = true;
            pictureBox1.Image = null;
            textBox1.Text = comboBox1.Text;
            Calculate();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            text = textBox1.Text;
            Calculate();
        }

        private void Calculate()
        {
            comboBox2.Items.Clear();
            label5.Visible = comboBox2.Visible = false;
            try
            {
                if (text.Contains('x'))
                {
                    Match match = Regex.Match(text, @"(-?\d*\,?\d*)x\s*([+-]*)\s*(\d*\,?\d*)\s*=\s*(-?\d*\,?\d*)");

                    if (match.Success)
                    {
                        double a = string.IsNullOrEmpty(match.Groups[1].Value) ? 1 : Convert.ToDouble(match.Groups[1].Value);
                        double b = string.IsNullOrEmpty(match.Groups[3].Value) ? 0 : Convert.ToDouble(match.Groups[3].Value);
                        double c = Convert.ToDouble(match.Groups[4].Value);
                        char sign = string.IsNullOrEmpty(match.Groups[2].Value) ? '+' : match.Groups[2].Value[0];

                        if (sign == '-')
                            b *= -1;

                        double x = (c - b) / a;
                        label3.Text = x.ToString();

                        comboBox2.Items.Add($"{c} - {b} = {c - b}");
                        comboBox2.Items.Add($"{c - b} / {a} = {x}");
                        label5.Visible = comboBox2.Visible = true;
                    }
                    else throw new Exception();
                }
                
                else
                {
                    text = text.Replace(',', '.');
                    string result = (new DataTable().Compute(text, null)).ToString();
                    if (result == "False")
                        throw new Exception();
                    label3.Text = result;
                }

                if (!comboBox1.Items.Contains(text) && text != string.Empty)
                {
                    comboBox1.Items.Add(text);

                    string MyCom = "insert into loghistory values(@login, @expression)";
                    var cmd1 = new SqlCommand(MyCom, MyConnection);
                    SqlParameter pr1 = new SqlParameter("@login", textBox2.Text);
                    cmd1.Parameters.Add(pr1);
                    SqlParameter pr2 = new SqlParameter("@expression", textBox1.Text);
                    cmd1.Parameters.Add(pr2);
                    MyConnection.Open();
                    cmd1.ExecuteNonQuery();
                    MyConnection.Close();
                }
            }
            catch
            {
                label3.Text = "Неверный формат";
            }            
        }

        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar >= '0' && e.KeyChar <= '9')
                return;
            if (e.KeyChar == (char)Keys.Back)
                return;
            if (e.KeyChar == '(' || e.KeyChar == ')' || e.KeyChar == ',' || e.KeyChar == ' ' || e.KeyChar == 'x')
                return;
            if (e.KeyChar == '+' || e.KeyChar == '/' || e.KeyChar == '-' || e.KeyChar == '*' || e.KeyChar == '=')
                return;
            e.KeyChar = '\0';
        }

        private void button3_Click(object sender, EventArgs e)
        {
            RefreshHistory();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            string MyCom = "delete from loghistory where login = @login";
            var cmd1 = new SqlCommand(MyCom, MyConnection);
            SqlParameter pr1 = new SqlParameter("@login", textBox2.Text);
            cmd1.Parameters.Add(pr1);
            MyConnection.Open();
            cmd1.ExecuteNonQuery();
            MyConnection.Close();

            RefreshHistory();
        }
    }
}