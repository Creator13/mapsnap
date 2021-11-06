/*
 * MIT LICENSE
 * 
 * Copyright 2015 Daniel S Wolf
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files
 * (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the
 * following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
 * CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
 * SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 *
 * https://gist.github.com/DanielSWolf/0ab6a96899cc5377bf54
 */

using System;
using System.Text;
using System.Threading;

namespace ConsoleTools;

/// <summary>
/// An ASCII progress bar
/// </summary>
public class ProgressBar : IDisposable, IProgress<int>
{
    private const int blockCount = 20;
    private readonly TimeSpan animationInterval = TimeSpan.FromSeconds(1.0 / 16);

    private readonly Timer timer;

    private readonly double maxValue;

    private int currentValue = 0;
    private string currentText = string.Empty;
    private bool disposed = false;
    private int animationIndex = 0;

    public ProgressBar(int maxValue)
    {
        this.maxValue = maxValue;
        
        timer = new Timer(TimerHandler);

        // A progress bar is only for temporary display in a console window.
        // If the console output is redirected to a file, draw nothing.
        // Otherwise, we'll end up with a lot of garbage in the target file.
        if (!Console.IsOutputRedirected)
        {
            ResetTimer();
        }
    }

    public void Report(int value)
    {
        Interlocked.Exchange(ref currentValue, value);
    }

    private void TimerHandler(object state)
    {
        lock (timer)
        {
            if (disposed) return;

            var progress = currentValue / maxValue;
            var progressBlockCount = (int)(progress * blockCount);
            var percent = (int)(progress * 100);
            var text = $"[{new string('#', progressBlockCount)}{new string(' ', blockCount - progressBlockCount)}] {percent,3}% ({currentValue}/{(int)maxValue})";
            UpdateText(text);

            ResetTimer();
        }
    }

    private void UpdateText(string text)
    {
        // Get length of common portion
        int commonPrefixLength = 0;
        int commonLength = Math.Min(currentText.Length, text.Length);
        while (commonPrefixLength < commonLength && text[commonPrefixLength] == currentText[commonPrefixLength])
        {
            commonPrefixLength++;
        }

        // Backtrack to the first differing character
        StringBuilder outputBuilder = new StringBuilder();
        outputBuilder.Append('\b', currentText.Length - commonPrefixLength);

        // Output new suffix
        outputBuilder.Append(text.Substring(commonPrefixLength));

        // If the new text is shorter than the old one: delete overlapping characters
        int overlapCount = currentText.Length - text.Length;
        if (overlapCount > 0)
        {
            outputBuilder.Append(' ', overlapCount);
            outputBuilder.Append('\b', overlapCount);
        }

        Console.Write(outputBuilder);
        currentText = text;
    }

    private void ResetTimer()
    {
        timer.Change(animationInterval, TimeSpan.FromMilliseconds(-1));
    }

    public void Dispose()
    {
        lock (timer)
        {
            disposed = true;
            UpdateText(string.Empty);
        }
    }
}
