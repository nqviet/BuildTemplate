#import <UIKit/UIKit.h>

// Laboon Mobile Haptics: UIImpactFeedbackGenerator bridge.
// All generator access is on the main queue. Never falls back to alert vibration.
static NSUInteger LaboonHapticGeneration = 0;
static UIImpactFeedbackGenerator *LaboonHapticGenerators[4];

static void LaboonHapticCancel(void)
{
    ++LaboonHapticGeneration;
    for (int i = 0; i < 4; ++i) LaboonHapticGenerators[i] = nil;
}

static void LaboonHapticObserveLifecycle(void)
{
    static dispatch_once_t once;
    dispatch_once(&once, ^{
        [NSNotificationCenter.defaultCenter addObserverForName:UIApplicationWillResignActiveNotification
            object:nil queue:NSOperationQueue.mainQueue usingBlock:^(NSNotification *note) {
                LaboonHapticCancel();
            }];
    });
}

static UIImpactFeedbackGenerator *LaboonHapticGeneratorForStyle(int style)
{
    // Matches HapticStyle: 0 Light, 1 Medium, 2 Soft (iOS 13+), 3 Heavy.
    int index = MIN(3, MAX(0, style));
    if (@available(iOS 10.0, *)) {
        if (LaboonHapticGenerators[index] == nil) {
            UIImpactFeedbackStyle feedbackStyle = index == 1 ? UIImpactFeedbackStyleMedium
                : index == 3 ? UIImpactFeedbackStyleHeavy : UIImpactFeedbackStyleLight;
            if (@available(iOS 13.0, *)) {
                if (index == 2) feedbackStyle = UIImpactFeedbackStyleSoft;
            }
            LaboonHapticGenerators[index] = [[UIImpactFeedbackGenerator alloc] initWithStyle:feedbackStyle];
        }
    }
    return LaboonHapticGenerators[index];
}

extern "C" void LaboonHapticsStop(void)
{
    if (NSThread.isMainThread) LaboonHapticCancel();
    else dispatch_async(dispatch_get_main_queue(), ^{ LaboonHapticCancel(); });
}

extern "C" void LaboonHapticsPrepare(int style)
{
    void (^prepare)(void) = ^{
        LaboonHapticObserveLifecycle();
        if (UIApplication.sharedApplication.applicationState != UIApplicationStateActive) return;
        [LaboonHapticGeneratorForStyle(style) prepare];
    };
    if (NSThread.isMainThread) prepare();
    else dispatch_async(dispatch_get_main_queue(), prepare);
}

extern "C" void LaboonHapticsPlay(int style, float intensity, int count, int gapMs)
{
    void (^play)(void) = ^{
        LaboonHapticObserveLifecycle();
        if (UIApplication.sharedApplication.applicationState != UIApplicationStateActive) return;
        ++LaboonHapticGeneration; // Replace pending pulses without discarding a prepared generator.
        if (@available(iOS 10.0, *)) {
            CGFloat strength = MIN(1.0, MAX(0.0, intensity));
            if (strength <= 0.0) return;
            UIImpactFeedbackGenerator *generator = LaboonHapticGeneratorForStyle(style);
            if (@available(iOS 13.0, *)) [generator impactOccurredWithIntensity:strength];
            else [generator impactOccurred];
            [generator prepare]; // Prepare for the next event, rather than immediately before this one.
            if (count > 1) {
                NSUInteger generation = LaboonHapticGeneration;
                int delayMs = MIN(200, MAX(60, gapMs));
                dispatch_after(dispatch_time(DISPATCH_TIME_NOW, (int64_t)delayMs * NSEC_PER_MSEC), dispatch_get_main_queue(), ^{
                    if (generation != LaboonHapticGeneration || UIApplication.sharedApplication.applicationState != UIApplicationStateActive) return;
                    if (@available(iOS 13.0, *)) [generator impactOccurredWithIntensity:strength];
                    else [generator impactOccurred];
                    [generator prepare];
                });
            }
        }
    };
    if (NSThread.isMainThread) play();
    else dispatch_async(dispatch_get_main_queue(), play);
}
