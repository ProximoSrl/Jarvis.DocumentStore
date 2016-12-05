(function (window, angular, undefined) {
    'use strict';

    angular
        .module('admin')
        .config(config);

    /**/ 
    function config($stateProvider, $urlRouterProvider) {
        //
        // For any unmatched url, redirect to /state1
        $urlRouterProvider.otherwise("/dashboard");
        //
        // Now set up the states
        $stateProvider
            .state('dashboard', {
                url: "/dashboard",
                templateUrl: "dashboard/dashboard.html",
                controller: "DashboardController as dashboard",
                data: { pageTitle: 'Dashboard', description: 'metrics' },
                resolve: {
                    configService : 'configService',
                    tenants: function (configService) {
                        return configService.getTenants();
                    },
                    version: function (configService) {
                        return configService.getVersion();
                    }
                }
            })
            .state('logs', {
                url: "/logs",
                templateUrl: "logs/logs.html",
                controller:"LogsController as logs",
                data : { pageTitle: 'Logs', description: 'what\'s happening...' }
            })
            .state('stream', {
                url: "/stream",
                templateUrl: "stream/stream.html",
                controller: "StreamController as stream",
                data : { pageTitle: 'Stream', description: 'inspect stream history' }
            })
            .state('scheduler', {
                url: "/scheduler",
                templateUrl: "scheduler/scheduler.html",
                controller: "SchedulerController as scheduler",
                data: { pageTitle: 'Scheduler' }
            })
            .state('recyclebin', {
                url: "/recylebin",
                templateUrl: "recyclebin/recyclebin.html",
                controller: "RecycleBinController as recyclebin",
                data: { pageTitle: 'Recycle Bin' }
            })
             .state('handles', {
                 url: "/handles",
                 templateUrl: "handles/handles.html",
                 controller: "HandlesController as handles",
                 data: { pageTitle: 'Manage Handles' }
             });
    }
})(window, window.angular);
