#import <UIKit/UIKit.h>
#include <stdio.h>
#include <stdlib.h>
extern "C" void LaboonHapticsPrepare(int);
extern "C" void LaboonHapticsPlay(int, float, int, int);
extern "C" void LaboonHapticsStop(void);
NSNotificationName const UIApplicationWillResignActiveNotification = @"HapticProbeResignActive";
static int allocations, prepares, impacts;
static void Check(bool value, const char *name) { if (!value) { fprintf(stderr, "FAIL: %s\n", name); exit(1); } printf("PASS: %s\n", name); }
@implementation UIApplication
+ (instancetype)sharedApplication { static UIApplication *app; static dispatch_once_t once; dispatch_once(&once, ^{ app = [UIApplication new]; }); return app; }
@end
@implementation UIImpactFeedbackGenerator
- (instancetype)initWithStyle:(UIImpactFeedbackStyle)style { self = [super init]; if (self) ++allocations; return self; }
- (void)prepare { Check(NSThread.isMainThread, "prepare on main thread"); ++prepares; }
- (void)impactOccurred { ++impacts; }
- (void)impactOccurredWithIntensity:(CGFloat)intensity { Check(NSThread.isMainThread, "impact on main thread"); ++impacts; }
@end
int main(void) {
    @autoreleasepool {
        LaboonHapticsPrepare(0);
        Check(allocations == 1 && prepares == 1 && impacts == 0, "prepare is silent and creates one cached generator");
        LaboonHapticsPlay(0, .25f, 1, 100);
        Check(allocations == 1 && impacts == 1 && prepares == 2, "play reuses prepared generator and warms next event");
        LaboonHapticsPlay(0, .4f, 2, 100);
        LaboonHapticsStop();
        int afterStop = impacts;
        dispatch_after(dispatch_time(DISPATCH_TIME_NOW, 150 * NSEC_PER_MSEC), dispatch_get_main_queue(), ^{
            Check(impacts == afterStop, "stop cancels pending second pulse");
            LaboonHapticsPrepare(0);
            Check(allocations == 2, "stop releases generator cache");
            LaboonHapticsPlay(0, .4f, 2, 100);
            LaboonHapticsPlay(1, .4f, 1, 100);
            int afterReplacement = impacts;
            dispatch_after(dispatch_time(DISPATCH_TIME_NOW, 150 * NSEC_PER_MSEC), dispatch_get_main_queue(), ^{
                Check(impacts == afterReplacement, "new pattern cancels old pending pulse");
                LaboonHapticsPlay(0, .4f, 2, 100);
                UIApplication.sharedApplication.applicationState = UIApplicationStateInactive;
                [NSNotificationCenter.defaultCenter postNotificationName:UIApplicationWillResignActiveNotification object:nil];
                int beforeBackground = impacts;
                int allocationsBeforeBackground = allocations;
                LaboonHapticsPrepare(0);
                LaboonHapticsPlay(0, .4f, 1, 100);
                dispatch_after(dispatch_time(DISPATCH_TIME_NOW, 150 * NSEC_PER_MSEC), dispatch_get_main_queue(), ^{
                    Check(impacts == beforeBackground && allocations == allocationsBeforeBackground, "background cancels pending pulse and rejects new feedback");
                    puts("Native control-flow probe passed; fake UIKit, no tactile or device verification.");
                    exit(0);
                });
            });
        });
    }
    for (;;) [[NSRunLoop currentRunLoop] runUntilDate:[NSDate dateWithTimeIntervalSinceNow:.1]];
}
