public class SimpleButton
{
    public bool IsPressed { get; private set; }

    public event System.Action Started;    // Called only on the initial press
    public event System.Action OnPressed;  // Can be called every frame while held, if needed
    public event System.Action OnReleased;

    private bool hasStarted = false;

    public void Press()
    {
        if (!IsPressed)
        {
            IsPressed = true;
            if (!hasStarted)
            {
                hasStarted = true;
                Started?.Invoke(); // Fire "first press" event
            }

            OnPressed?.Invoke();
        }
    }

    public void Release()
    {
        if (IsPressed)
        {
            IsPressed = false;
            OnReleased?.Invoke();
            hasStarted = false; // Reset for next press
        }
    }
}
