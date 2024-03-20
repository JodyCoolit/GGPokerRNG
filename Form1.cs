using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Timers;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace GGPokerRNG
{
    public partial class Form1 : Form
    {
        private int xOffset = 15; // Horizontal offset from the left border of the table
        private int yOffset = -15;
        private int labelWidth = 100; // Width of the label
        private int labelHeight = 50; // Height of the label
        private int refreshLocationInterval = 1000;
        private int refreshTable = 1000;
        private int refreshNumber = 30000;

        private string GGPokerProcessName = "GGnet";
        private string GGPokerTableNameEN = "Rush & Cash";
        private string GGPokerTableNameCN = "¼«ËÙ&ºì°ü×À";

        private System.Windows.Forms.Timer tableCheckTimer;
        private System.Windows.Forms.Timer labelNumberTimer;
        private System.Windows.Forms.Timer labelLocationTimer;

        private uint GGPokerProcessId;
        private Dictionary<IntPtr, Label> tableLabels;

        // Constants for window styles
        private const int WS_EX_LAYERED = 0x80000;
        private const int WS_EX_TRANSPARENT = 0x20;
        private const int HTCAPTION = 0x2;
        private const int WM_NCHITTEST = 0x84;
        private const int HTCLIENT = 0x1;

        private readonly Random random = new Random();

        private Dictionary<IntPtr, DateTime> lastVisibilityChangeTimes = new Dictionary<IntPtr, DateTime>();
        private TimeSpan visibilityChangeThreshold = TimeSpan.FromSeconds(1); // 1-second threshold for example

        public Form1()
        {
            InitializeComponent();
            InitializeFormStyles();
            tableLabels = new Dictionary<IntPtr, Label>();
            this.ShowInTaskbar = false;

            try
            {
                GGPokerProcessId = GetGGPokerProcessId(GGPokerProcessName);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            ConfigureTray();
            StartTableCheckTimer();
            //StartLabelUpdateTimer();
            StartLabelLocationTimer();
        }

        private void StartLabelLocationTimer()
        {
            labelLocationTimer = new System.Windows.Forms.Timer();
            labelLocationTimer.Interval = refreshLocationInterval; // Set the interval in milliseconds
            labelLocationTimer.Tick += new EventHandler(UpdateLabelPosition); // Use Tick event
            labelLocationTimer.Start();
        }

        private void UpdateLabelPosition(object? sender, EventArgs e)
        {
            IntPtr foregroundWindow = GetForegroundWindow();
            this.Invoke((MethodInvoker)delegate
            {
                foreach (var kvp in tableLabels)
                {
                    IntPtr hWnd = kvp.Key;
                    Label label = kvp.Value;
                    if (!IsWindowCovered(hWnd))
                    {
                        if (GetWindowRect(hWnd, out RECT windowRect))
                        {
                            label.Location = new Point(windowRect.Left + xOffset, windowRect.Bottom - label.Height + yOffset);
                        }
                    }
                }
            });
        }

        private void ChangeNumberWithEffect(Label label, int newValue)
        {
            int currentValue = int.Parse(label.Text);
            int totalSteps = 5; // Fixed number of steps
            int step = (newValue - currentValue) / totalSteps;
            int stepsCount = 0; // Track the number of steps taken

            // If the difference is too small, force a minimum step value (either 1 or -1)
            if (step == 0)
            {
                step = newValue > currentValue ? 1 : -1;
            }

            System.Windows.Forms.Timer animationTimer = new System.Windows.Forms.Timer();
            animationTimer.Interval = 50; // Set the interval for the changes (50 milliseconds for example)

            animationTimer.Tick += (sender, args) =>
            {
                if (stepsCount < totalSteps)
                {
                    currentValue += step;
                    label.Text = currentValue.ToString();
                    stepsCount++;
                }
                else
                {
                    // Directly set to the final value to avoid overshooting due to step size
                    label.Text = newValue.ToString();
                    animationTimer.Stop();
                }
            };

            animationTimer.Start();
        }



        private void StartLabelUpdateTimer()
        {
            labelNumberTimer = new System.Windows.Forms.Timer();
            labelNumberTimer.Interval = refreshNumber; // Interval in milliseconds
            labelNumberTimer.Tick += new EventHandler(UpdateLabelWithRandomNumber); // Use Tick event
            labelNumberTimer.Start();
        }

        private void UpdateLabelWithRandomNumber(object? sender, EventArgs e)
        {
            foreach (var kvp in tableLabels)
            {
                IntPtr hWnd = kvp.Key;
                Label label = kvp.Value;

                if (label.Visible == false)
                {
                    continue;
                }

                this.Invoke((MethodInvoker)delegate
                {
                    if (GetWindowRect(hWnd, out RECT windowRect))
                    {
                        // Window is visible - update label
                        label.Text = random.Next(1, 100).ToString();
                    }
                });
            }
        }


        private void InitializeFormStyles()
        {
            this.FormBorderStyle = FormBorderStyle.None; // Remove title bar and borders
            this.WindowState = FormWindowState.Maximized; // Set the form to fullscreen

            // Set a nearly transparent black color for the form's background
            this.BackColor = Color.FromArgb(1, 1, 1);

            // Make the form fully transparent except for the controls
            this.TransparencyKey = Color.FromArgb(1, 1, 1); // Unique color for transparency
            this.Opacity = 1; // Fully opaque
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            this.TopMost = true; // Ensure the form stays on top of other windows
        }


        // Override WndProc to handle window messages
        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            switch (m.Msg)
            {
                case WM_NCHITTEST:
                    // Allow dragging the form by clicking and dragging anywhere on it
                    m.Result = (IntPtr)(HTCAPTION);
                    break;
            }
        }

        // P/Invoke declarations for Win32 API functions
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong);


        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                //cp.ExStyle |= WS_EX_TRANSPARENT | WS_EX_LAYERED; // Set window style to transparent and layered
                return cp;
            }
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        private void ConfigureTray()
        {
            // Assuming you have an .ico file named "newIcon.ico" in the application's directory
            string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tray_icon.ico");
            notifyIcon1.Icon = new Icon(iconPath);
            notifyIcon1.Text = "GGPoker Table Overlay";
            notifyIcon1.Visible = true;

            contextMenuStrip1.Items.Add("Exit", null, Exit_Click);
            notifyIcon1.ContextMenuStrip = contextMenuStrip1;
        }


        private void StartTableCheckTimer()
        {
            tableCheckTimer = new System.Windows.Forms.Timer();
            tableCheckTimer.Interval = refreshTable; // Interval in milliseconds
            tableCheckTimer.Tick += new EventHandler(CheckForOpenTables); // Use Tick event
            tableCheckTimer.Start();
        }

        private void CreateOverlayLabelForTable(IntPtr hWnd, string title)
        {
            Debug.WriteLine($"Creating overlay label for table with title: {title}");

            if (!tableLabels.ContainsKey(hWnd))
            {
                RECT windowRect;
                if (GetWindowRect(hWnd, out windowRect))
                {
                    Label tableLabel = new Label
                    {
                        Text = random.Next(1, 100).ToString(),
                        AutoSize = false,
                        TextAlign = ContentAlignment.MiddleCenter,
                        Location = new Point(windowRect.Left + xOffset, windowRect.Bottom - labelHeight + yOffset),
                        Size = new Size(labelWidth, labelHeight),
                        BorderStyle = BorderStyle.FixedSingle,
                        BackColor = Color.FromArgb(106, 90, 205),
                        ForeColor = Color.WhiteSmoke,
                        Font = new Font("Tahoma", 14, FontStyle.Bold),
                        Padding = new Padding(6),
                        Visible = false, // Set initial visibility to false
                    };
                    tableLabel.MouseDown += TableLabel_Click;

                    // Shadow effect
                    tableLabel.Paint += (sender, e) =>
                    {
                        e.Graphics.DrawRectangle(new Pen(Color.Black, 4), tableLabel.DisplayRectangle);
                    };

                    // Use Invoke to add the label on the UI thread
                    this.Invoke((MethodInvoker)delegate
                    {
                        this.Controls.Add(tableLabel);
                        tableLabels[hWnd] = tableLabel;
                        this.Refresh(); // Refresh the form after adding the label
                    });

                    Debug.WriteLine($"Label created and added for table with handle: {hWnd}");
                }
            }
            else
            {
                Debug.WriteLine($"Label already exists for table with handle: {hWnd}");
            }
        }

        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string? lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        private void TableLabel_Click(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (sender is Label label)
                {
                    int newValue = random.Next(1, 100);
                    ChangeNumberWithEffect(label, newValue);
                }
            }
        }

        [DllImport("user32.dll")]
        static extern bool IsIconic(IntPtr hWnd);

        private bool IsWindowMinimized(IntPtr hWnd)
        {
            return IsIconic(hWnd);
        }

        private void CheckForOpenTables(object? sender, EventArgs e)
        {
            IntPtr foregroundWindow = GetForegroundWindow();
            var windows = WindowHelper.GetOpenWindows();

            foreach (var hWnd in windows)
            {
                // Get the process ID for the window
                uint windowProcessId;
                WindowHelper.GetWindowThreadProcessId(hWnd, out windowProcessId);

                // Continue only if the window belongs to the GGpoker application
                if (windowProcessId != GGPokerProcessId) continue;

                // Get the title of the window and skip if it matches the specific title
                string title = WindowHelper.GetWindowTitle(hWnd);

                if (title == null)
                    continue;

                if (!title.StartsWith(GGPokerTableNameEN) && !title.StartsWith(GGPokerTableNameCN))
                    continue;

                // Determine if the window is covered (includes checks for minimized and non-visible states)
                bool windowCovered = IsWindowCovered(hWnd);
                bool windowForeground = (hWnd == foregroundWindow);

                // If the window is the foreground window or it's not covered, show the label
                if (windowForeground || !windowCovered)
                {
                    if (!tableLabels.ContainsKey(hWnd))
                    {
                        CreateOverlayLabelForTable(hWnd, title);
                    }
                    tableLabels[hWnd].Visible = true;
                }
                else if (tableLabels.ContainsKey(hWnd))
                {
                    // If the window is covered, hide the label
                    tableLabels[hWnd].Visible = false;
                }
            }
        }


        private uint GetGGPokerProcessId(string partialProcessName)
        {
            Process[] processes = Process.GetProcesses();
            Process? matchingProcess = Array.Find(processes, p => p.ProcessName.Contains(partialProcessName));

            if (matchingProcess != null)
            {
                return (uint)matchingProcess.Id;
            }
            else
            {
                throw new InvalidOperationException($"No process with a name containing {partialProcessName} found.");
            }
        }

        private void Exit_Click(object? sender, EventArgs e)
        {
            notifyIcon1.Visible = false;
            Application.Exit();
        }

        // Windows API imports
        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {

        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        private bool IsWindowCovered(IntPtr hWnd)
        {
            if (!WindowHelper.IsWindowVisible(hWnd) || IsWindowMinimized(hWnd))
            {
                return true; // Minimized or not visible windows are considered "covered"
            }

            GetWindowRect(hWnd, out RECT rect);
            var windowArea = (rect.Right - rect.Left) * (rect.Bottom - rect.Top);

            int coveredArea = 0;
            IntPtr currentHwnd = GetForegroundWindow(); // Start with the foreground window

            while (currentHwnd != IntPtr.Zero && currentHwnd != hWnd)
            {
                if (WindowHelper.IsWindowVisible(currentHwnd))
                {
                    GetWindowRect(currentHwnd, out RECT currentRect);
                    if (IntersectRect(out RECT coveredRect, ref rect, ref currentRect))
                    {
                        int area = (coveredRect.Right - coveredRect.Left) * (coveredRect.Bottom - coveredRect.Top);
                        coveredArea += area;
                    }
                }

                if (coveredArea > windowArea)
                {
                    return true; // Consider it covered if more than half is obscured
                }

                currentHwnd = GetWindow(currentHwnd, GW_HWNDPREV); // Move to the next lower window in Z-order
            }

            return false; // Not covered if no window has covered more than half of hWnd
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

        private const uint GW_HWNDPREV = 3;

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IntersectRect(out RECT lprcDst, [In] ref RECT lprcSrc1, [In] ref RECT lprcSrc2);
    }
}
