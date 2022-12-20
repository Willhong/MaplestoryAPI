using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MaplestoryAPI
{
    public partial class Form1 : Form
    {
        class CubeDetail
        {

            public string id { get; set; } = "";
            public string character_name { get; set; } = "";
            public string create_date { get; set; } = "";
            public string cube_type { get; set; } = "";
            public string item_upgrade_result { get; set; } = "";
            public string miracle_time_flag { get; set; } = "";
            public string item_equip_part { get; set; } = "";
            public string item_level { get; set; } = "";
            public string target_item { get; set; } = "";
            public string potential_option_grade { get; set; } = "";
            public string additional_potential_option_grade { get; set; } = "";
            public (string, string)[] before_potential_options { get; set; } = new (string, string)[3];
            public (string, string)[] after_potential_options { get; set; } = new (string, string)[3];


        }
        public struct potential_options
        {
            public string value { get; set; }
            public string grade { get; set; }

        }
        class CubeHistory
        {
            public CubeDetail cube_histories { get; set; }
            public string next_cursor { get; set; }
        }

        public Form1()
        {
            InitializeComponent();
        }

        CubeDetail cd2 = new CubeDetail();
        

        string Key;
        string result = string.Empty;
        string responseFromServer = string.Empty;
        int progress_bar_Value = 0;
        List<CubeHistory> ListRawCubeHistory;

        public void GetData(int count, DateTime Fromdate, DateTime ToDate, string cursor)
        {
            
            object obj = new object();
            object obj2 = new object();
            DateTime _ToDate = ToDate;
            DateTime _Fromdate = Fromdate;
            ListRawCubeHistory = new List<CubeHistory>();
            TimeSpan subTime = _ToDate.Subtract(_Fromdate);
            pBar.Maximum = subTime.Days+1;
            Type type = typeof(CubeDetail);
            FieldInfo[] f = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Parallel.For(0, subTime.Days, index =>
            {
                
                string strDate = (_ToDate.AddDays(-index)).ToString("yyyy-MM-dd");

                string targetURL = $"https://public.api.nexon.com/openapi/maplestory/v1/cube-use-results?count={count}&date={strDate}&cursor={cursor}";

                WebRequest request = WebRequest.Create(targetURL);
                request.Method = "GET";
                request.ContentType = "application/json";
                request.Headers.Add("authorization", Key);
                try
                {
                    using (WebResponse response = request.GetResponse())
                    using (Stream dataStream = response.GetResponseStream())
                    using (StreamReader reader = new StreamReader(dataStream))
                    {
                        CubeDetail cd = new CubeDetail();
                        string tmpData = reader.ReadToEnd();
                        CubeHistory ch = new CubeHistory();
                        dynamic item = JsonConvert.DeserializeObject(tmpData);
                        dynamic cube_histories = item["cube_histories"];
                        dynamic next_cursor = item["next_cursor"];
                        foreach (dynamic value in cube_histories)
                        {
                            foreach (dynamic value2 in value)
                            {
                                JProperty v2 = value2;
                                foreach (FieldInfo f2 in f)
                                {
                                    if (f2.Name.Contains(v2.Name))
                                    {
                                        if (f2.Name.Contains("potential_options"))
                                        {
                                            (string, string)[] tmpArr = new (string, string)[3];
                                            int cnt = 0;
                                            foreach (JToken val in v2.Value)
                                            {
                                                tmpArr[cnt++] = (val["value"].ToString(), val["grade"].ToString());

                                            }
                                            f2.SetValue(cd, tmpArr);
                                            break;
                                        }
                                        f2.SetValue(cd, v2.Value.ToString());
                                        break;
                                    }

                                }
                            }
                            ch.cube_histories = cd;
                            ch.next_cursor = next_cursor;
                            lock(obj){
                                ListRawCubeHistory.Add(ch);
                                
                            }
                            

                        }

                    }

                }
                catch (Exception e)
                {
                    File.WriteAllText(Application.StartupPath+@"\error1.txt",e.ToString());
                }

            });

            dataGridView1.Columns.Clear();
            foreach (FieldInfo f2 in f)
            {
                Char[] splitchar = { '>', '<' };
                if (f2.Name.Split(splitchar)[1].Contains("_potential_options"))
                {
                    for (int i = 1; i < 4; i++)
                    {
                        dataGridView1.Columns.Add(f2.Name.Split(splitchar)[1] + i, f2.Name.Split(splitchar)[1] + i);
                    }

                }
                else
                {
                    dataGridView1.Columns.Add(f2.Name.Split(splitchar)[1], f2.Name.Split(splitchar)[1]);
                }
                
            }
            foreach (CubeHistory ch in ListRawCubeHistory)
            {
                object[] a = new object[dataGridView1.Columns.Count];

                for (int i = 0; i < f.Length; i++)
                {
                    FieldInfo f2 = f[i];
                    if (f2.Name.Contains("_potential_options"))
                    {

                        int idx = 0;
                        if (f2.Name.Contains("after"))
                            idx = 2;
                        (string, string)[] tmp = ((string, string)[])f2.GetValue(ch.cube_histories);
                        a[idx + i] = tmp[0];
                        a[idx + i + 1] = tmp[1];
                        a[idx + i + 2] = tmp[2];

                    }
                    else
                    {
                        a[i] = f2.GetValue(ch.cube_histories);
                    }

                }
                dataGridView1.Rows.Add(a);
            }
        }

     

        private void btnSearch_Click(object sender, EventArgs e)
        {
            if (Key ==null)
            {
                try
                {
                    GetKey();
                }
                catch
                {
                    OpenFileDialog ofd = new OpenFileDialog();
                    ofd.ShowDialog();
                    Key=File.ReadAllText(ofd.FileName);
                }
            }

            GetData(1000, dtpFrom.Value, dtpTo.Value, "");
            
        }

        private void GetKey()
        {
            Key = File.ReadAllText(Application.StartupPath + @"\key.txt");
        }

        private void btnReadKey_Click(object sender, EventArgs e)
        {
            try
            {
                GetKey();
            }
            catch
            {
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.ShowDialog();
                Key = File.ReadAllText(ofd.FileName);
            }
        }
    }
}
