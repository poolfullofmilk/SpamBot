using System.ComponentModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SpamBot.Services;

namespace SpamBot.ViewModels;

internal partial class MainViewModel : ObservableObject
{
    private const int ArmingSeconds = 3;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartCommand))]
    private string _message = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SpeedLabel))]
    [NotifyPropertyChangedFor(nameof(EstimatedCountLabel))]
    private int _messagesPerSecond = 100;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DurationLabel))]
    [NotifyPropertyChangedFor(nameof(EstimatedCountLabel))]
    private int _durationSeconds = 1;

    [ObservableProperty]
    private string _status = "Idle";

    [ObservableProperty]
    private double _progressPercentage;

    public string SpeedLabel =>
        MessagesPerSecond == 1 ? "1 Message / Second" : $"{MessagesPerSecond} Messages / Second";

    public string DurationLabel =>
        DurationSeconds switch
        {
            1 => "1 Second",
            60 => "1 Minute",
            _ => $"{DurationSeconds} Seconds",
        };

    public string EstimatedCountLabel
    {
        get
        {
            int estimated = DurationSeconds * MessagesPerSecond;
            return estimated == 1
                ? "Will Send About 1 Message"
                : $"Will Send About {estimated} Messages";
        }
    }

    private bool CanStart() => !string.IsNullOrWhiteSpace(Message);

    [RelayCommand(CanExecute = nameof(CanStart), IncludeCancelCommand = true)]
    private async Task StartAsync(CancellationToken cancellationToken)
    {
        int sentCount = 0;

        try
        {
            // Grace Period To Focus The Target Window
            using (PeriodicTimer armingTimer = new(TimeSpan.FromSeconds(1)))
            {
                for (int secondsLeft = ArmingSeconds; secondsLeft > 0; secondsLeft--)
                {
                    Status = $"Click Target Window, Starting In {secondsLeft}s";
                    await armingTimer.WaitForNextTickAsync(cancellationToken);
                }
            }

            TimeSpan duration = TimeSpan.FromSeconds(DurationSeconds);
            using PeriodicTimer sendTimer = new(TimeSpan.FromSeconds(1d / MessagesPerSecond));
            long startedAt = Stopwatch.GetTimestamp();

            while (await sendTimer.WaitForNextTickAsync(cancellationToken))
            {
                TimeSpan elapsed = Stopwatch.GetElapsedTime(startedAt);
                if (elapsed >= duration)
                {
                    break;
                }

                KeystrokeSender.SendLine(Message);
                sentCount++;
                ProgressPercentage = elapsed / duration * 100;
                Status = $"Sending, {(duration - elapsed).TotalSeconds:0}s Left, {sentCount} Sent";
            }

            Status = $"Finished, {sentCount} Sent";
        }
        catch (OperationCanceledException)
        {
            Status = $"Stopped, {sentCount} Sent";
        }
        catch (Win32Exception)
        {
            // Focused Window Is Usually Running Elevated
            Status = "Windows Blocked The Keystrokes";
        }
        finally
        {
            ProgressPercentage = 0;
        }
    }

    [RelayCommand]
    private void Reset()
    {
        Message = string.Empty;
        MessagesPerSecond = 100;
        DurationSeconds = 1;
        ProgressPercentage = 0;
        Status = "Idle";
    }
}
