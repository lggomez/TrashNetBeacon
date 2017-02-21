using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

using nsWin32Calls;

namespace TrashNetBeacon
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            RunApplicationWithMutex();
        }

        private static TrashNetBeacon form;

        // Credit: http://www.pinvoke.net/default.aspx/kernel32.createmutex
        private static void RunApplicationWithMutex()
        {
            // create IntPtrs for use with CreateMutex()
            IntPtr mutexAttributesIp = new IntPtr(0);
            IntPtr mutexHandle = new IntPtr(0);

            try
            {
                // Create the mutex and verify its status BEFORE construction
                // of the main form.
                mutexHandle = Win32Calls.CreateMutex(mutexAttributesIp, true, "TrashNetBeacon_MUTEX");

                if (mutexHandle != IntPtr.Zero)
                {
                    int lastWin32Error = Marshal.GetLastWin32Error();

                    // if we get the ERROR_ALREADY_EXISTS value, there is 
                    // already another instance of this application running.
                    if (lastWin32Error == Win32Calls.ERROR_ALREADY_EXISTS)
                    {
                        MessageBox.Show(
                            "There is already an open instance of this program",
                            "Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error,
                            MessageBoxDefaultButton.Button1);

                        // So, don't allow this instance to run.
                        return;
                    }
                }
                else
                {
                    // CreateMutex() failed.
                }

                form = new TrashNetBeacon();
                Application.Run(form);
            }
            catch (Exception ex)
            {
                Debug.Assert(true, $"Error while manipulating mutex: {ex.Message}");
            }
            finally
            {
                // Release the mutex
                if (mutexHandle != (IntPtr)0) Win32Calls.ReleaseMutex(mutexHandle);

                // Cleanup the main form object instance.
                form?.Dispose();
            }
        }
    }
}

namespace nsWin32Calls
{
    /// <summary>
    /// Win32 calls encapsulation object.
    /// </summary>
    public class Win32Calls
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr CreateMutex(IntPtr lpMutexAttributes, bool bInitialOwner, string lpName);

        [DllImport("kernel32.dll")]
        public static extern bool ReleaseMutex(IntPtr hMutex);

        /// <summary>
        /// This value can be returned by CreateMutex() and is found in
        /// C++ in the error.h header file.
        /// </summary>
        public const int ERROR_ALREADY_EXISTS = 183;
    }
}