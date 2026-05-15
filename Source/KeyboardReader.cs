using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SharpDX;
using SharpDX.DirectInput;

namespace Metacraft.VcsHardware;

internal sealed class KeyboardReader : IDisposable
{
	private const int BUFFER_SIZE = 128;

	// DirectInput "device is gone" HRESULTs. An unplugged or otherwise
	// inaccessible device surfaces as one of these; they are expected
	// operational states, not errors.
	private const int DIERR_INPUTLOST = unchecked((int)0x8007001E);
	private const int DIERR_NOTACQUIRED = unchecked((int)0x8007001C);
	private const int DIERR_UNPLUGGED = unchecked((int)0x80040209);

	private readonly string mKeyboardName;
	private readonly int mVendorId;
	private readonly int mProductId;
	private readonly ILogger mLogger;
	private readonly Action mConnectedAction;
	private readonly Action mDisconnectedAction;
	private readonly Action<int> mKeyPressedAction;
	private readonly Action<int> mKeyReleasedAction;
	private readonly Action<Exception> mErrorAction;
	private readonly Timer mCheckTimer;
	private readonly DirectInput mDirectInput = new();
	private readonly object mScanLock = new();
	private Joystick? mDevice;
	private AutoResetEvent? mWaitHandle;
	private RegisteredWaitHandle? mRegistration;
	private int mIsDisposed;
	private int mScanActive;

	public bool IsKeyboardPresent
	{
		get
		{
			if (Volatile.Read(ref mIsDisposed) != 0) {
				return false;
			}
			return Volatile.Read(ref mScanActive) != 0;
		}
	}

	public KeyboardReader(
		string keyboardName,
		int vendorId,
		int productId,
		ILogger logger,
		Action connectedAction,
		Action disconnectedAction,
		Action<int> keyPressedAction,
		Action<int> keyReleasedAction,
		Action<Exception> errorAction
	)
	{
		mKeyboardName = keyboardName;
		mVendorId = vendorId;
		mProductId = productId;
		mLogger = logger ?? NullLogger.Instance;
		mConnectedAction = connectedAction;
		mDisconnectedAction = disconnectedAction;
		mKeyPressedAction = keyPressedAction;
		mKeyReleasedAction = keyReleasedAction;
		mErrorAction = errorAction;

		mCheckTimer = new Timer(DoCheck);
		mCheckTimer.Change(1000, Timeout.Infinite);
	}

	private void DoCheck(object? _)
	{
		if (Volatile.Read(ref mIsDisposed) != 0) {
			return;
		}

		bool justConnected = false;
		try {
			lock (mScanLock) {
				if (Volatile.Read(ref mIsDisposed) != 0) {
					return;
				}

				if (mDevice is null) {
					CheckForDevice();
					if (mDevice is not null) {
						StartScan();
						justConnected = Volatile.Read(ref mScanActive) != 0;
					}
				}
			}

			if (justConnected && Volatile.Read(ref mIsDisposed) == 0) {
				mConnectedAction();
			}
		}
		catch (Exception ex) {
			mLogger?.LogError(ex, "Unexpected error during device check");
			mErrorAction(ex);
		}
		finally {
			// Re-arm the timer; guard against the race where Dispose ran
			// between our disposed check and here.
			try {
				if (Volatile.Read(ref mIsDisposed) == 0) {
					mCheckTimer.Change(1000, Timeout.Infinite);
				}
			}
			catch (ObjectDisposedException) { }
		}
	}

	private void CheckForDevice()
	{
		mDevice = null;

		foreach (Guid deviceGuid in mDirectInput
			.GetDevices(DeviceClass.All, DeviceEnumerationFlags.AllDevices)
			.Where(d => d.Type == DeviceType.Supplemental)
			.Select(d => d.InstanceGuid)
			.Where(mDirectInput.IsDeviceAttached)
		) {
			Joystick? device = null;
			try {
				device = new Joystick(mDirectInput, deviceGuid);
				if (device.Properties.VendorId == mVendorId && device.Properties.ProductId == mProductId) {
					mDevice = device;
					device = null;
					mLogger?.LogDebug("{KeyboardName} found", mKeyboardName);
					return;
				}
			}
			catch (SharpDXException ex) {
				mLogger?.LogError(ex, "Error inspecting candidate device");
			}
			finally {
				device?.Dispose();
			}
		}

		mLogger?.LogDebug("{KeyboardName} not found", mKeyboardName);
	}

	private void StartScan()
	{
		if (mDevice is null) {
			return;
		}

		try {
			mWaitHandle = new AutoResetEvent(initialState: false);
			mRegistration = ThreadPool.RegisterWaitForSingleObject(
				mWaitHandle,
				OnSignaled,
				state: null,
				timeout: Timeout.InfiniteTimeSpan,
				executeOnlyOnce: false
			);

			mDevice.Properties.BufferSize = BUFFER_SIZE;
			mDevice.SetNotification(mWaitHandle);
			mDevice.Acquire();
			Volatile.Write(ref mScanActive, 1);
		}
		catch (Exception ex) {
			mLogger?.LogError(ex, "Failed to start scan; tearing down partial state");
			TeardownScan();
		}
	}

	private void TeardownScan()
	{
		Volatile.Write(ref mScanActive, 0);

		// Pass null here rather than waiting on a handle: this method may
		// be called from inside OnSignaled (the error path), and waiting
		// for the callback to finish from within itself would deadlock.
		// Dispose() does the wait-for-completion teardown separately.
		mRegistration?.Unregister(null);
		mRegistration = null;

		try { mDevice?.SetNotification(null); } catch { }
		try { mDevice?.Unacquire(); } catch { }
		try { mDevice?.Dispose(); } catch { }
		mDevice = null;

		mWaitHandle?.Dispose();
		mWaitHandle = null;
	}

	private static bool IsDeviceLost(SharpDXException ex)
	{
		int hr = ex.HResult;
		return hr == DIERR_INPUTLOST || hr == DIERR_NOTACQUIRED || hr == DIERR_UNPLUGGED;
	}

	private void HandleDisconnect()
	{
		// Only the first caller to flip 1 -> 0 wins, so concurrent OnSignaled
		// callbacks coalesce into a single disconnect event and a single teardown.
		if (Interlocked.Exchange(ref mScanActive, 0) == 0) {
			return;
		}

		// Take the lock to serialize with timer-driven StartScan, but skip
		// if we can't get it immediately — Dispose may be in progress and
		// holding the lock while waiting for us to return.
		if (Monitor.TryEnter(mScanLock)) {
			try {
				if (Volatile.Read(ref mIsDisposed) == 0) {
					TeardownScan();
				}
			}
			finally {
				Monitor.Exit(mScanLock);
			}
		}

		if (Volatile.Read(ref mIsDisposed) == 0) {
			mDisconnectedAction();
		}
	}

	private void OnSignaled(object? state, bool timedOut)
	{
		if (Volatile.Read(ref mIsDisposed) != 0) {
			return;
		}

		// A concurrent callback may have already torn down the scan; bail
		// before we touch a device that's being disposed.
		if (Volatile.Read(ref mScanActive) == 0) {
			return;
		}

		// Snapshot the device reference. The field can be nulled by the timer
		// thread or a sibling error path, but the local won't change under us.
		Joystick? device = mDevice;
		if (device is null) {
			return;
		}

		try {
			foreach (JoystickUpdate update in device.GetBufferedData()) {
				// Buttons0..Buttons127 are contiguous in the JoystickOffset enum.
				if (update.Offset < JoystickOffset.Buttons0 || update.Offset > JoystickOffset.Buttons127) {
					continue;
				}

				int buttonIndex = update.Offset - JoystickOffset.Buttons0;
				bool pressed = (update.Value & 0x80) != 0;

				if (pressed) {
					mKeyPressedAction(buttonIndex);
				} else {
					mKeyReleasedAction(buttonIndex);
				}
			}
		}
		catch (SharpDXException ex) when (IsDeviceLost(ex)) {
			mLogger?.LogInformation("{KeyboardName} disconnected", mKeyboardName);
			HandleDisconnect();
		}
		catch (Exception ex) {
			mLogger?.LogError(ex, "Error reading device data, stopping scan");
			mErrorAction(ex);
			HandleDisconnect();
		}
	}

	public void Dispose()
	{
		if (Interlocked.Exchange(ref mIsDisposed, 1) != 0) {
			return;
		}

		// Stop the timer first so no new DoCheck callbacks start. Dispose with
		// a wait handle to block until any in-flight tick completes.
		using (ManualResetEvent timerStopped = new(false)) {
			if (!mCheckTimer.Dispose(timerStopped)) {
				// Already disposed somehow; nothing to wait on.
			} else {
				timerStopped.WaitOne();
			}
		}

		// Now serialize with any in-flight OnSignaled. Unregister with a wait
		// handle to block until the callback finishes; this is safe here
		// because Dispose is never called from OnSignaled itself.
		lock (mScanLock) {
			Volatile.Write(ref mScanActive, 0);

			if (mRegistration is not null) {
				using ManualResetEvent unregistered = new(initialState: false);
				mRegistration.Unregister(unregistered);
				unregistered.WaitOne();
				mRegistration = null;
			}

			try { mDevice?.SetNotification(null); } catch { }
			try { mDevice?.Unacquire(); } catch { }
			try { mDevice?.Dispose(); } catch { }
			mDevice = null;

			try { mDirectInput.Dispose(); } catch { }

			mWaitHandle?.Dispose();
			mWaitHandle = null;
		}
	}
}
