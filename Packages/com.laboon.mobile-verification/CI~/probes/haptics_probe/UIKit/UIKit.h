#import <Foundation/Foundation.h>
#import <dispatch/dispatch.h>
typedef NS_ENUM(NSInteger, UIImpactFeedbackStyle) { UIImpactFeedbackStyleLight, UIImpactFeedbackStyleMedium, UIImpactFeedbackStyleHeavy, UIImpactFeedbackStyleSoft, UIImpactFeedbackStyleRigid };
typedef NS_ENUM(NSInteger, UIApplicationState) { UIApplicationStateActive, UIApplicationStateInactive };
extern NSNotificationName const UIApplicationWillResignActiveNotification;
@interface UIApplication : NSObject
@property(nonatomic) UIApplicationState applicationState;
+ (instancetype)sharedApplication;
@end
@interface UIImpactFeedbackGenerator : NSObject
- (instancetype)initWithStyle:(UIImpactFeedbackStyle)style;
- (void)prepare;
- (void)impactOccurred;
- (void)impactOccurredWithIntensity:(CGFloat)intensity;
@end
