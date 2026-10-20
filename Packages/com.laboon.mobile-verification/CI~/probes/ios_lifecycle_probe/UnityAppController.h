#import <UIKit/UIKit.h>

// Test double for Unity's native trampoline. The production bridge is compiled unchanged.
@interface UnityAppController : UIResponder <UIApplicationDelegate>
@property(nonatomic, strong) UIWindow *window;
- (UIWindowScene *)pickStartupWindowScene:(NSSet<UIScene *> *)scenes;
@end

extern const char *AppControllerClassName;
#define IMPL_APP_CONTROLLER_SUBCLASS(ClassName) \
    @implementation ClassName (OverrideAppDelegate) \
    + (void)load { AppControllerClassName = #ClassName; } \
    @end
