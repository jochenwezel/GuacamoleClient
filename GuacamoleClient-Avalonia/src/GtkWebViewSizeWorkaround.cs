using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace GuacClient
{
    internal static class GtkWebViewSizeWorkaround
    {
        private const string GtkLibrary = "libgtk-3.so.0";
        private const string GLibLibrary = "libglib-2.0.so.0";
        private static readonly GLibSourceCallback s_resizeCallback = ApplyQueuedSize;

        public static Task ApplyAsync(IntPtr webView, int width, int height)
        {
            var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var invocation = new ResizeInvocation(webView, width, height, completion);
            GCHandle invocationHandle = GCHandle.Alloc(invocation);
            IntPtr invocationPointer = GCHandle.ToIntPtr(invocationHandle);

            if (GLibIdleAdd(s_resizeCallback, invocationPointer) != 0)
                return completion.Task;

            invocationHandle.Free();
            completion.SetException(new InvalidOperationException("Unable to queue the WebKitGTK resize."));
            return completion.Task;
        }

        private static int ApplyQueuedSize(IntPtr data)
        {
            GCHandle invocationHandle = GCHandle.FromIntPtr(data);
            var invocation = (ResizeInvocation)invocationHandle.Target!;
            invocationHandle.Free();

            try
            {
                ApplySize(invocation.WebView, invocation.Width, invocation.Height);
                invocation.Completion.TrySetResult(true);
            }
            catch (Exception ex)
            {
                invocation.Completion.TrySetException(ex);
            }

            return 0;
        }

        private static void ApplySize(IntPtr webView, int width, int height)
        {
            var allocation = new GtkAllocation(0, 0, width, height);
            IntPtr widget = webView;

            for (int level = 0; widget != IntPtr.Zero && level < 16; level++)
            {
                GtkWidgetSetSizeRequest(widget, width, height);
                GtkWidgetSizeAllocate(widget, ref allocation);
                GtkWidgetQueueResize(widget);
                GtkWidgetQueueDraw(widget);
                widget = GtkWidgetGetParent(widget);
            }

            IntPtr topLevel = GtkWidgetGetTopLevel(webView);
            if (topLevel != IntPtr.Zero)
                GtkWindowResize(topLevel, width, height);
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int GLibSourceCallback(IntPtr data);

        [StructLayout(LayoutKind.Sequential)]
        private struct GtkAllocation
        {
            public GtkAllocation(int x, int y, int width, int height)
            {
                X = x;
                Y = y;
                Width = width;
                Height = height;
            }

            public int X;
            public int Y;
            public int Width;
            public int Height;
        }

        private sealed record ResizeInvocation(
            IntPtr WebView,
            int Width,
            int Height,
            TaskCompletionSource<bool> Completion);

        [DllImport(GLibLibrary, EntryPoint = "g_idle_add")]
        private static extern uint GLibIdleAdd(GLibSourceCallback callback, IntPtr data);

        [DllImport(GtkLibrary, EntryPoint = "gtk_widget_get_parent")]
        private static extern IntPtr GtkWidgetGetParent(IntPtr widget);

        [DllImport(GtkLibrary, EntryPoint = "gtk_widget_get_toplevel")]
        private static extern IntPtr GtkWidgetGetTopLevel(IntPtr widget);

        [DllImport(GtkLibrary, EntryPoint = "gtk_widget_set_size_request")]
        private static extern void GtkWidgetSetSizeRequest(IntPtr widget, int width, int height);

        [DllImport(GtkLibrary, EntryPoint = "gtk_widget_size_allocate")]
        private static extern void GtkWidgetSizeAllocate(IntPtr widget, ref GtkAllocation allocation);

        [DllImport(GtkLibrary, EntryPoint = "gtk_widget_queue_resize")]
        private static extern void GtkWidgetQueueResize(IntPtr widget);

        [DllImport(GtkLibrary, EntryPoint = "gtk_widget_queue_draw")]
        private static extern void GtkWidgetQueueDraw(IntPtr widget);

        [DllImport(GtkLibrary, EntryPoint = "gtk_window_resize")]
        private static extern void GtkWindowResize(IntPtr window, int width, int height);
    }
}
