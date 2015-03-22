namespace Oxide.Plugins
{
    [Info("Unity Sample Plugin", "bawNg", 0.1)]
    class SamplePlugin : CSharpPlugin
    {
        private int currentFrames;
        private float currentDuration;

        void Loaded()
        {
            Puts("SamplePlugin: This plugin will display the average frame rate for any Unity game");
        }

        void OnFrame(float delta)
        {
            currentFrames++;
            currentDuration += delta;

            if (currentDuration >= 15f)
            {
                Puts("Average frame rate over last 15 seconds: {0:0.00}", (float)currentFrames / currentDuration);
                currentFrames = 0;
                currentDuration = 0f;
            }
        }
    }
}