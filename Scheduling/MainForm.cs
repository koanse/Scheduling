using System;
using System.Collections.Generic;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Scheduling
{
    public partial class MainForm : Form
    {
        int prodNumG, prodNumTau, prodNumD;
        string stdText;
        ExMatrix[] amG, amTau, amD;
        IterationState[] iters;
        string[] varNames;
        public MainForm()
        {
            InitializeComponent();
            InitializeControls(new SchData(1, 1, 1, 1, null, 0.5f));
            stdText = "Введите исходные данные и " +
                "выберете пункт меню 'Оптимизация' -> 'Оптимизировать'.";
            webBrowser1.DocumentText = stdText;
        }
        
        void nud_ValueChanged(object sender, EventArgs e)
        {
            int n, m, L, T;
            n = (int)nudN.Value;
            m = (int)nudM.Value;
            L = (int)nudL.Value;
            T = (int)nudT.Value;
            float alpha = float.Parse(tbAlpha.Text);
            SchData data = new SchData(n, m, L, T, null, alpha);
            InitializeControls(data);
        }
        void mdgvTheta_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                int thetaInit = 5;
                double value =
                    Math.Round((double)mdgvTheta[e.ColumnIndex, e.RowIndex].Value);
                mdgvTheta[e.ColumnIndex, e.RowIndex].Value = value;
                if (value <= 0 || value > 100)
                    mdgvTheta[e.ColumnIndex, e.RowIndex].Value = thetaInit;
                int n, m, L, T;
                n = (int)nudN.Value;
                m = (int)nudM.Value;
                L = (int)nudL.Value;
                T = (int)nudT.Value;
                float alpha = float.Parse(tbAlpha.Text);
                SchData data = new SchData(n, m, L, T, mdgvTheta.Matrix, alpha);
                InitializeControls(data);
            }
            catch { }
        }
        void tbAlpha_TextChanged(object sender, EventArgs e)
        {
            try
            {
                float.Parse(tbAlpha.Text);
            }
            catch
            {
                tbAlpha.Text = "0,5";
            }
        }
        void nudProdG_ValueChanged(object sender, EventArgs e)
        {
            amG[prodNumG] = mdgvG.Matrix;
            prodNumG = (int)nudProdG.Value - 1;
            mdgvG.Matrix = amG[prodNumG];
        }
        void nudProdTau_ValueChanged(object sender, EventArgs e)
        {
            amTau[prodNumTau] = mdgvTau.Matrix;
            prodNumTau = (int)nudProdTau.Value - 1;
            mdgvTau.Matrix = amTau[prodNumTau];
        }
        void nudProdD_ValueChanged(object sender, EventArgs e)
        {
            amD[prodNumD] = mdgvD.Matrix;
            prodNumD = (int)nudProdD.Value - 1;
            mdgvD.Matrix = amD[prodNumD];
        }
        void lbIters_SelectedValueChanged(object sender, EventArgs e)
        {
            int i = lbIters.SelectedIndex;
            if (iters == null || i < 0 ||
                i >= iters.Length || iters.Length == 0 ||
                varNames == null)
                return;
            webBrowser1.DocumentText = iters[i].GetReport(varNames, false);
        }
        
        SchData ReadSchData()
        {
            SchData data = new SchData();
            data.n = (int)nudN.Value;
            data.m = (int)nudM.Value;
            data.L = (int)nudL.Value;
            data.T = (int)nudT.Value;
            data.alpha = float.Parse(tbAlpha.Text);
            data.mTheta = mdgvTheta.Matrix;
            data.mP = mdgvP.Matrix;
            data.mQMin = mdgvQMin.Matrix;
            data.mQMax = mdgvQMax.Matrix;
            data.mB = mdgvB.Matrix;
            data.mN = mdgvN.Matrix;
            data.amG = amG;
            int i = (int)nudProdG.Value - 1;
            data.amG[i] = mdgvG.Matrix;
            data.amTau = amTau;
            i = (int)nudProdTau.Value - 1;
            data.amTau[i] = mdgvTau.Matrix;
            data.amD = amD;
            i = (int)nudProdD.Value - 1;
            data.amD[i] = mdgvD.Matrix;
            return data;
        }
        void InitializeControls(SchData data)
        {
            nudN.Value = data.n;
            nudM.Value = data.m;
            nudL.Value = data.L;
            nudT.Value = data.T;
            tbAlpha.Text = data.alpha.ToString();
            mdgvTheta.Matrix = data.mTheta;
            mdgvP.Matrix = data.mP;
            mdgvQMin.Matrix = data.mQMin;
            mdgvQMax.Matrix = data.mQMax;
            mdgvB.Matrix = data.mB;
            mdgvN.Matrix = data.mN;
            nudProdG.Maximum = data.amG.Length;
            nudProdG.Value = 1;
            prodNumG = 0;
            amG = new ExMatrix[data.amG.Length];
            for (int i = 0; i < amG.Length; i++)
                amG[i] = new ExMatrix(new ExMatrix[] { data.amG[i] });
            mdgvG.Matrix = amG[0];
            nudProdTau.Maximum = data.amTau.Length;
            nudProdTau.Value = 1;
            prodNumTau = 0;
            amTau = new ExMatrix[data.amTau.Length];
            for (int i = 0; i < amTau.Length; i++)
                amTau[i] = new ExMatrix(new ExMatrix[] { data.amTau[i] });
            mdgvTau.Matrix = amTau[0];
            nudProdD.Maximum = data.amD.Length;
            nudProdD.Value = 1;
            prodNumD = 0;
            amD = new ExMatrix[data.amD.Length];
            for (int i = 0; i < amD.Length; i++)
                amD[i] = new ExMatrix(new ExMatrix[] { data.amD[i] });
            mdgvD.Matrix = amD[0];
            
        }
        
        void optimizeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SchData data = ReadSchData();
            // Переменных
            int varCount = 3 * data.m * data.T + 2 * data.L * data.T;
            int sum = 0;
            for (int i = 0; i < data.n; i++)                
                sum += data.T + (int)data.mTheta.Elements[i, 0] - 1;
            varCount += 2 * sum;

            // Ограничений
            int eqCount = 2 * data.m * data.T + 2 * data.L * data.T +
                sum + data.n;
            
            // Cуммы по i количеств периодов планирования до i-го изделия
            ExMatrix mSum = new ExMatrix(data.n, 1);
            for (int i = 1; i < data.n; i++)
                mSum.Elements[i, 0] = mSum.Elements[i - 1, 0] +
                    data.T + (int)data.mTheta.Elements[i, 0] - 1;

            // Целевая функция
            ExMatrix mC = new ExMatrix(1, varCount);
            for (int j = 0; j < data.m; j++)
                for (int t = 0; t < data.T; t++)
                    mC.Elements[0, j * data.T + t] = -data.mP.Elements[j, t];            
            
            // Столбцы коэффициентов ограничений
            ExMatrix[] amP = new ExMatrix[varCount];
            for (int i = 0; i < amP.Length; i++)
                amP[i] = new ExMatrix(eqCount, 1);
            
            // Столбец свободных членов
            ExMatrix mA = new ExMatrix(eqCount, 1);

            // 1
            int eqIndex = 0;
            for (int l = 0; l < data.L; l++)
                for (int t = 0; t < data.T; t++)
                {
                    // x
                    int varOffset = data.m * data.T;
                    for (int i = 0; i < data.n; i++)
                        for (int k = 0; k < data.mTheta.Elements[i, 0]; k++)
                        {
                            int index = varOffset + (int)mSum.Elements[i, 0] + t + k;
                            amP[index].Elements[eqIndex, 0] += data.amG[i].Elements[l, k];
                        }

                    // zQMin
                    varOffset = data.m * data.T + sum;
                    amP[varOffset + l * data.T + t].Elements[eqIndex, 0] = -1;

                    // QMin
                    mA.Elements[eqIndex, 0] = data.mQMin.Elements[l, t];
                    eqIndex++;
                }

            // 2
            for (int l = 0; l < data.L; l++)
                for (int t = 0; t < data.T; t++)
                {
                    // x
                    int varOffset = data.m * data.T;
                    for (int i = 0; i < data.n; i++)
                        for (int k = 0; k < data.mTheta.Elements[i, 0]; k++)
                        {
                            int index = varOffset + (int)mSum.Elements[i, 0] + t + k;
                            amP[index].Elements[eqIndex, 0] += data.amG[i].Elements[l, k];
                        }

                    // zQMax
                    varOffset = data.m * data.T + sum + data.L * data.T;
                    amP[varOffset + l * data.T + t].Elements[eqIndex, 0] = 1;

                    // QMax
                    mA.Elements[eqIndex, 0] = data.mQMax.Elements[l, t];
                    eqIndex++;
                }

            // 3
            for (int j = 0; j < data.m; j++)
                for (int t = 0; t < data.T; t++)
                {
                    // y
                    amP[j * data.T + t].Elements[eqIndex, 0] = -1;

                    // x
                    int varOffset = data.m * data.T;
                    for (int i = 0; i < data.n; i++)
                        for (int k = 0; k < data.mTheta.Elements[i, 0]; k++)
                        {
                            int index = varOffset + (int)mSum.Elements[i, 0] + t + k;
                            amP[index].Elements[eqIndex, 0] += data.amTau[i].Elements[j, k];
                        }

                    // zB
                    varOffset = data.m * data.T + sum + 2 * data.L * data.T;
                    amP[varOffset + j * data.T + t].Elements[eqIndex, 0] = 1;

                    // B
                    mA.Elements[eqIndex, 0] = data.mB.Elements[j, t];
                    eqIndex++;
                }

            // 4
            for (int i = 0; i < data.n; i++)
                for (int t = 0; t < data.T + data.mTheta.Elements[i, 0] - 1; t++)
                {
                    // x
                    int varOffset = data.m * data.T;
                    int index = varOffset + (int)mSum.Elements[i, 0] + t;
                    amP[index].Elements[eqIndex, 0] = 1;

                    // zD
                    varOffset = 2 * data.m * data.T + sum + 2 * data.L * data.T;
                    amP[varOffset + (int)mSum.Elements[i, 0] + t].Elements[eqIndex, 0] = -1;

                    // D
                    mA.Elements[eqIndex, 0] = data.amD[i].Elements[t, 0];
                    eqIndex++;
                }

            // 5
            for (int i = 0; i < data.n; i++)
            {
                // x
                int varOffset = data.m * data.T;
                for (int t = 0; t < data.T; t++)
                    amP[varOffset + (int)mSum.Elements[i, 0] + t].Elements[eqIndex, 0] = 1;

                // N
                mA.Elements[eqIndex, 0] = data.mN.Elements[i, 0];
                eqIndex++;
            }

            // 6
            for (int j = 0; j < data.m; j++)
                for (int t = 0; t < data.T; t++)
                {
                    // y
                    amP[j * data.m + t].Elements[eqIndex, 0] = 1;

                    // zAlphaB
                    int varOffset = 2 * data.m * data.T + 2 * sum +
                        2 * data.L * data.T;
                    amP[varOffset + j * data.T + t].Elements[eqIndex, 0] = 1;
                    
                    // AlphaB
                    mA.Elements[eqIndex, 0] = data.alpha * data.mB.Elements[j, t];
                    eqIndex++;
                }

            MsMethod method = new MsMethod(amP, mA, mC, 0);
            int[] basis = new int[eqCount];
            int pos, varNum = data.m * data.T + 2 * data.L * data.T + sum;
            for (pos = 0; pos < varNum; pos++)
                basis[pos] = data.m * data.T + sum + pos;
            for (int i = 0; i < data.n; i++)
                basis[pos + i] = data.m * data.T + (int)mSum.Elements[i, 0];
            pos += data.n;
            for (int i = 0; i < data.m * data.T; i++)
            {
                basis[pos] = 2 * data.m * data.T + 2 * data.L * data.T + 2 * sum + i;
                pos++;
            }

            webBrowser1.DocumentText = stdText;
            lbIters.Items.Clear();
            try
            {
                method.SetBasis(basis);
                while (method.DoIteration()) ;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Вычисления прерваны",
                    MessageBoxButtons.OK, MessageBoxIcon.Stop);
            }

            iters = (IterationState[])method.states.ToArray(typeof(IterationState));
            lbIters.SuspendLayout();
            for (int i = 0; i < iters.Length; i++)
            {
                string str;
                if (iters[i].isDecisionLegal)
                    str = string.Format("{0}. F = {1}; допустимое решение",
                        i + 1, -iters[i].GetFuncValue());
                else
                    str = string.Format("{0}. F = {1}; недопустимое решение",
                        i + 1, -iters[i].GetFuncValue());
                lbIters.Items.Add(str);
            }
            lbIters.ResumeLayout();
            varNames = GetHtmlVarNames(data);
            try
            {
                lbIters.SelectedIndex = lbIters.Items.Count - 1;
            }
            catch { }
            tabControl1.SelectedTab = tabControl1.TabPages["tpResults"];
        }
        void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            saveFileDialog1.FileName = "task1.sch";
            saveFileDialog1.Filter = "Файлы задач|*.sch";
            try
            {
                if (saveFileDialog1.ShowDialog() != DialogResult.OK)
                    return;
                string filename = saveFileDialog1.FileName;
                SchData data = ReadSchData();
                FileStream fs = new FileStream(filename, FileMode.Create);
                BinaryFormatter bf = new BinaryFormatter();
                bf.Serialize(fs, data);
                fs.Close();
            }
            catch
            {
                MessageBox.Show("Ошибка сохранения файла", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (openFileDialog1.ShowDialog() != DialogResult.OK)
                    return;
                string filename = openFileDialog1.FileName;
                FileStream fs = new FileStream(filename, FileMode.Open);
                BinaryFormatter bf = new BinaryFormatter();
                SchData data = (SchData)bf.Deserialize(fs);
                fs.Close();
                InitializeControls(data);
            }
            catch
            {
                MessageBox.Show("Ошибка открытия файла", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }
        void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AboutBox ab = new AboutBox();
            ab.ShowDialog();
        }
        void saveReportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            saveFileDialog1.FileName = "report1.html";
            saveFileDialog1.Filter = "Файлы отчетов|*.html";
            try
            {
                if (saveFileDialog1.ShowDialog() != DialogResult.OK)
                    return;
                string filename = saveFileDialog1.FileName;
                FileStream fs = new FileStream(filename, FileMode.Create);
                StreamWriter sw = new StreamWriter(fs);
                for (int i = 0; i < iters.Length; i++)
                {
                    sw.Write(string.Format("<P>ИТЕРАЦИЯ {0}</P>", i + 1));
                    sw.Write(iters[i].GetReport(varNames, false));
                }
                sw.Close();
            }
            catch
            {
                MessageBox.Show("Ошибка сохранения отчета", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        void saveTaskReportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            saveFileDialog1.FileName = "taskreport1.html";
            saveFileDialog1.Filter = "Файлы отчетов об условии задачи|*.html";
            try
            {
                if (saveFileDialog1.ShowDialog() != DialogResult.OK)
                    return;
                string filename = saveFileDialog1.FileName;
                FileStream fs = new FileStream(filename, FileMode.Create);
                StreamWriter sw = new StreamWriter(fs);
                SchData data = ReadSchData();
                sw.Write("<P>РАЗМЕРНОСТЬ</P>");
                sw.Write(string.Format("<P>Видов продукции (n): {0}</P>", data.n));
                sw.Write(string.Format("<P>Групп оборудования (m): {0}</P>", data.m));
                sw.Write(string.Format("<P>Видов ресурсов (L): {0}</P>", data.L));
                sw.Write(string.Format("<P>Интервалов планирования (T): {0}</P>", data.T));
                sw.Write("<P>Длительности производственных циклов изготовления продукции (θ):</P>");
                sw.Write(data.mTheta.ToHtml());

                sw.Write("<P>ПЕРЕГРУЗКИ</P>");
                sw.Write(string.Format("<P>Относительное значение перегрузки (α): {0}</P>", data.alpha));
                sw.Write("<P>Штрафные коэффициенты перегрузок (P):</P>");
                sw.Write(data.mP.ToHtml());

                sw.Write("<P>РЕСУРСЫ</P>");
                sw.Write("<P>Нормы расходов ресурсов (g):</P>");
                for (int i = 0; i < data.n; i++)
                {
                    sw.Write(string.Format("<P>Номер вида продукции (i): {0}</P>", i + 1));
                    sw.Write(data.amG[i].ToHtml());
                }

                sw.Write("<P>Минимальный расход ресурсов (q):</P>");
                sw.Write(data.mQMin.ToHtml());

                sw.Write("<P>Запасы ресурсов (Q):</P>");
                sw.Write(data.mQMax.ToHtml());
                
                sw.Write("<P>ТРУДОЕМКОСТИ</P>");
                sw.Write("<P>Трудоемкости изготовления продукции (τ):</P>");
                for (int i = 0; i < data.n; i++)
                {
                    sw.Write(string.Format("<P>Номер вида продукции (i): {0}</P>", i + 1));
                    sw.Write(data.amTau[i].ToHtml());
                }

                sw.Write("<P>Фонды времени работы групп оборудования (B):</P>");
                sw.Write(data.mB.ToHtml());

                sw.Write("<P>ВЫПУСК</P>");
                sw.Write("<P>Минимальные объемы выпуска продукции (d):</P>");
                for (int i = 0; i < data.n; i++)
                {
                    sw.Write(string.Format("<P>Номер вида продукции (i): {0}</P>", i + 1));
                    sw.Write(data.amD[i].ToHtml());
                }
                sw.Write("<P>Объемы выпуска продукции за период планирования (N):</P>");
                sw.Write(data.mN.ToHtml());
                sw.Close();
            }
            catch
            {
                MessageBox.Show("Ошибка сохранения отчета об условии задачи", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        string[] GetHtmlVarNames(SchData data)
        {
            ArrayList res = new ArrayList();
            for (int j = 0; j < data.m; j++)
                for (int t = 0; t < data.T; t++)
                    res.Add(string.Format("y<SUP>п</SUP><SUB>{0},{1}</SUB>", j + 1, t + 1));

            for (int i = 0; i < data.n; i++)
                for (int t = 0; t < data.T + data.mTheta.Elements[i, 0] - 1; t++)
                    res.Add(string.Format("x<SUB>{0},{1}</SUB>", i + 1, t + 1));

            for (int l = 0; l < data.L; l++)
                for (int t = 0; t < data.T; t++)
                    res.Add(string.Format("zq<SUB>{0},{1}</SUB>", l + 1, t + 1));

            for (int l = 0; l < data.L; l++)
                for (int t = 0; t < data.T; t++)
                    res.Add(string.Format("zQ<SUB>{0},{1}</SUB>", l + 1, t + 1));

            for (int j = 0; j < data.m; j++)
                for (int t = 0; t < data.T; t++)
                    res.Add(string.Format("zB<SUB>{0},{1}</SUB>", j + 1, t + 1));

            for (int i = 0; i < data.n; i++)
                for (int t = 0; t < data.T + data.mTheta.Elements[i, 0] - 1; t++)
                    res.Add(string.Format("zd<SUB>{0},{1}</SUB>", i + 1, t + 1));

            for (int j = 0; j < data.m; j++)
                for (int t = 0; t < data.T; t++)
                    res.Add(string.Format("zαB<SUB>{0},{1}</SUB>", j + 1, t + 1));

            return (string[])res.ToArray(typeof(string));
        }        
    }
    public class MatrixDataGridView : DataGridView
    {
        string rowText = "", colText = "";
        public string RowHeaderStartText
        {
            get { return rowText; }
            set { rowText = value; }
        }
        public string ColumnHeaderStartText
        {
            get { return colText; }
            set { colText = value; }
        }
        public ExMatrix Matrix
        {
            set
            {
                if (value == null)
                    return;
                if (Rows.Count != value.M ||
                    Columns.Count != value.N)
                {
                    SuspendLayout();
                    Rows.Clear();
                    Columns.Clear();
                    for (int j = 0; j < value.N; j++)
                    {
                        DataGridViewColumn c =
                            new DataGridViewColumn(new DataGridViewTextBoxCell());
                        c.ValueType = typeof(double);
                        int colNum = j + 1;
                        c.HeaderText = colText + colNum.ToString();
                        Columns.Add(c);
                    }
                    Rows.Add(value.M);
                    for (int i = 0; i < value.M; i++)
                    {
                        int rowNum = i + 1;
                            Rows[i].HeaderCell.Value = rowText + rowNum.ToString();
                        for (int j = 0; j < value.N; j++)
                            Rows[i].Cells[j].Value = value.Elements[i, j];
                    }
                    ResumeLayout();
                }
                else
                {
                    for (int i = 0; i < value.M; i++)
                        for (int j = 0; j < value.N; j++)
                            Rows[i].Cells[j].Value = value.Elements[i, j];
                }
            }
            get
            {
                if (Rows.Count == 0 || Columns.Count == 0)
                    return null;
                ExMatrix res = new ExMatrix(Rows.Count, Columns.Count);
                for (int i = 0; i < Rows.Count; i++)
                    for (int j = 0; j < Columns.Count; j++)
                        res.Elements[i, j] = (double)Rows[i].Cells[j].Value;
                return res;
            }
        }
        public MatrixDataGridView()
        {
            this.AllowUserToAddRows = false;
            this.AllowUserToDeleteRows = false;            
            this.BackgroundColor = Color.LightGray;
        }
        protected override void OnDataError(bool displayErrorDialogIfNoHandler,
            DataGridViewDataErrorEventArgs e)
        {
            MessageBox.Show("Неверный формат данных. Для дробных чисел используйте запятую",
                "Ошибка ввода данных", MessageBoxButtons.OK, MessageBoxIcon.Error);
            base.OnDataError(false, e);
        }
    }
    [Serializable]
    public class SchData
    {
        public int n, m, L, T;
        public float alpha;
        public ExMatrix mTheta, mP, mQMin, mQMax, mB, mN;
        public ExMatrix[] amG, amTau, amD;
        public SchData() { }
        public SchData(int n, int m, int L, int T,
            ExMatrix mTheta, float alpha)
        {
            this.n = n;
            this.m = m;
            this.T = T;
            this.L = L;
            this.alpha = alpha;
            if (mTheta == null)
            {
                int thetaInit = 5;
                this.mTheta = new ExMatrix(n, 1);
                for (int i = 0; i < this.mTheta.M; i++)
                    this.mTheta.Elements[i, 0] = thetaInit;
            }
            else
                this.mTheta = new ExMatrix(new ExMatrix[] { mTheta });
            mP = new ExMatrix(m, T);
            mQMin = new ExMatrix(L, T);
            mQMax = new ExMatrix(L, T);
            mB = new ExMatrix(m, T);
            mN = new ExMatrix(n, 1);
            amG = new ExMatrix[n];
            for (int i = 0; i < amG.Length; i++)
                amG[i] = new ExMatrix(L, (int)this.mTheta.Elements[i, 0]);
            amTau = new ExMatrix[n];
            for (int i = 0; i < amTau.Length; i++)
                amTau[i] = new ExMatrix(m, (int)this.mTheta.Elements[i, 0]);
            amD = new ExMatrix[n];
            for (int i = 0; i < amD.Length; i++)
                amD[i] = new ExMatrix(T + (int)this.mTheta.Elements[i, 0] - 1, 1);
        }
    }
}