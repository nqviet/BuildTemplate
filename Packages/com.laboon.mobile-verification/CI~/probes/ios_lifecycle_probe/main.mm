#import "UnityAppController.h"

const char *AppControllerClassName = "UnityAppController";
static NSInteger initializationCount;

static void Record(NSString *event)
{
    NSString *path = [NSSearchPathForDirectoriesInDomains(NSDocumentDirectory, NSUserDomainMask, YES).firstObject
        stringByAppendingPathComponent:@"lifecycle-events.txt"];
    NSString *previous = [NSString stringWithContentsOfFile:path encoding:NSUTF8StringEncoding error:nil] ?: @"";
    [[previous stringByAppendingFormat:@"%@\n", event] writeToFile:path atomically:YES encoding:NSUTF8StringEncoding error:nil];
}

@implementation UnityAppController
- (UIWindowScene *)pickStartupWindowScene:(NSSet<UIScene *> *)scenes
{
    for (UIScene *scene in scenes)
        if ([scene isKindOfClass:UIWindowScene.class]) return (UIWindowScene *)scene;
    return nil;
}

- (BOOL)application:(UIApplication *)application didFinishLaunchingWithOptions:(NSDictionary *)options
{
    UIWindowScene *scene = [self pickStartupWindowScene:application.connectedScenes];
    NSAssert(scene != nil, @"Unity must initialize only after its UIWindowScene exists");
    initializationCount++;
    NSAssert(initializationCount == 1, @"Unity initialized twice");
    self.window = [[UIWindow alloc] initWithWindowScene:scene];
    UIViewController *controller = [UIViewController new];
    controller.view.backgroundColor = UIColor.systemTealColor;
    self.window.rootViewController = controller;
    [self.window makeKeyAndVisible];
    Record(@"initialized-with-scene");
    return YES;
}
- (void)applicationDidBecomeActive:(UIApplication *)application
{
    NSAssert(self.window.windowScene != nil && initializationCount == 1, @"Invalid active state");
    Record(@"active");
}
- (void)applicationWillResignActive:(UIApplication *)application { Record(@"inactive"); }
- (void)applicationDidEnterBackground:(UIApplication *)application { Record(@"background"); }
- (void)applicationWillEnterForeground:(UIApplication *)application { Record(@"foreground"); }
@end

int main(int argc, char **argv)
{
    @autoreleasepool { return UIApplicationMain(argc, argv, nil, [NSString stringWithUTF8String:AppControllerClassName]); }
}
