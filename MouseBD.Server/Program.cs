using MouseBD.Server;

// Single instance check
var mutex = new Mutex(true, "MouseBD_Server_Mutex", out bool isNew);
if (!isNew)
{
    MessageBox.Show("MouseBD Server jest już uruchomiony.", "MouseBD", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    return;
}

Application.EnableVisualStyles();
Application.SetCompatibleTextRenderingDefault(false);
Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);

Application.Run(new TrayApp());

mutex.ReleaseMutex();
