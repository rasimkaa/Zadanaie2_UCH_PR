using System;
using System.Windows;
using System.Windows.Controls;

namespace WpfApp1
{
    public partial class MainWindow : Window
    {
        TextBox[] obj;
        TextBox[,] cons;
        TextBox[] rhs;
        int vars, consCount;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void CreateForm_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(txtVars.Text, out vars) || vars < 1 || vars > 10) return;
            if (!int.TryParse(txtCons.Text, out consCount) || consCount < 1 || consCount > 10) return;

            contentPanel.Children.Clear();

            // ---- Целевая функция ----
            var fPanel = new StackPanel { Margin = new Thickness(0, 10, 0, 10) };
            fPanel.Children.Add(new TextBlock { Text = "Целевая функция:", FontSize = 18 });

            var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 5, 0, 5) };
            obj = new TextBox[vars];
            for (int i = 0; i < vars; i++)
            {
                obj[i] = new TextBox { Width = 60, Text = "0", Margin = new Thickness(2) };
                row.Children.Add(obj[i]);
                row.Children.Add(new TextBlock { Text = $"·x{i + 1}  " });
            }
            row.Children.Add(new TextBlock
            {
                Text = (cmbType.SelectedIndex == 0 ? "→ max" : "→ min"),
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(10, 0, 0, 0)
            });
            fPanel.Children.Add(row);
            contentPanel.Children.Add(fPanel);


            // ---- Ограничения ----
            cons = new TextBox[consCount, vars];
            rhs = new TextBox[consCount];

            var consTitle = new TextBlock { Text = "Ограничения:", FontSize = 18 };
            contentPanel.Children.Add(consTitle);

            for (int i = 0; i < consCount; i++)
            {
                var cRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 5, 0, 5) };

                for (int j = 0; j < vars; j++)
                {
                    cons[i, j] = new TextBox { Width = 60, Text = "0", Margin = new Thickness(2) };
                    cRow.Children.Add(cons[i, j]);
                    cRow.Children.Add(new TextBlock { Text = $"·x{j + 1}  " });
                }

                cRow.Children.Add(new TextBlock { Text = "≤", Margin = new Thickness(10, 0, 10, 0) });

                rhs[i] = new TextBox { Width = 60, Text = "0" };
                cRow.Children.Add(rhs[i]);

                contentPanel.Children.Add(cRow);
            }

            var solveBtn = new Button
            {
                Content = "Решить",
                Width = 150,
                Height = 40,
                Background = System.Windows.Media.Brushes.SeaGreen,
                Foreground = System.Windows.Media.Brushes.White,
                Margin = new Thickness(0, 15, 0, 0)
            };
            solveBtn.Click += Solve_Click;

            contentPanel.Children.Add(solveBtn);
        }

        private void Solve_Click(object sender, RoutedEventArgs e)
        {
            double[] c = new double[vars];
            double[,] A = new double[consCount, vars];
            double[] b = new double[consCount];

            try
            {
                for (int j = 0; j < vars; j++)
                    c[j] = double.Parse(obj[j].Text);

                for (int i = 0; i < consCount; i++)
                {
                    for (int j = 0; j < vars; j++)
                        A[i, j] = double.Parse(cons[i, j].Text);

                    b[i] = double.Parse(rhs[i].Text);
                }
            }
            catch
            {
                MessageBox.Show("Ошибка ввода данных");
                return;
            }

            bool isMax = cmbType.SelectedIndex == 0;

            var (solution, value) = SimplexSolve(c, A, b, isMax);

            string result = "Оптимальное решение:\n";
            for (int i = 0; i < solution.Length; i++)
                result += $"x{i + 1} = {solution[i]:F4}\n";
            result += $"\nF = {value:F4}";

            MessageBox.Show(result, "Результат");
        }

        // ------------------------------------------------------------------------
        //              УПРОЩЁННЫЙ СИМПЛЕКС-МЕТОД БЕЗ ИСТОРИИ
        // ------------------------------------------------------------------------
        private (double[], double) SimplexSolve(double[] c, double[,] A, double[] b, bool maximize)
        {
            int m = A.GetLength(0);
            int n = A.GetLength(1);

            int total = n + m;
            double[,] T = new double[m + 1, total + 1];

            for (int i = 0; i < m; i++)
            {
                for (int j = 0; j < n; j++)
                    T[i, j] = A[i, j];

                T[i, n + i] = 1;
                T[i, total] = b[i];
            }

            for (int j = 0; j < n; j++)
                T[m, j] = maximize ? -c[j] : c[j];

            int[] basis = new int[m];
            for (int i = 0; i < m; i++) basis[i] = n + i;

            while (true)
            {
                int pivotCol = -1;
                double min = 0;
                for (int j = 0; j < total; j++)
                {
                    if (T[m, j] < min)
                    {
                        min = T[m, j];
                        pivotCol = j;
                    }
                }
                if (pivotCol == -1) break;

                int pivotRow = -1;
                double best = double.MaxValue;

                for (int i = 0; i < m; i++)
                {
                    if (T[i, pivotCol] > 1e-9)
                    {
                        double ratio = T[i, total] / T[i, pivotCol];
                        if (ratio < best)
                        {
                            best = ratio;
                            pivotRow = i;
                        }
                    }
                }
                if (pivotRow == -1) throw new Exception("Неограниченная функция");

                double p = T[pivotRow, pivotCol];
                for (int j = 0; j <= total; j++)
                    T[pivotRow, j] /= p;

                for (int i = 0; i <= m; i++)
                {
                    if (i == pivotRow) continue;
                    double f = T[i, pivotCol];
                    for (int j = 0; j <= total; j++)
                        T[i, j] -= f * T[pivotRow, j];
                }

                basis[pivotRow] = pivotCol;
            }

            double[] X = new double[n];
            for (int i = 0; i < m; i++)
                if (basis[i] < n) X[basis[i]] = T[i, total];

            double val = T[m, total];
            if (!maximize) val = -val;

            return (X, val);
        }
    }
}
