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
                data : { pageTitle: 'Dashboard', description: 'metrics' }
            })
            .state('logs', {
                url: "/logs",
                templateUrl: "logs/logs.html",
                controller:"LogsController as logs",
                data : { pageTitle: 'Logs', description: 'what\'s happening...' }
            })
            .state('info', {
                url: "/info",
                templateUrl: "info/info.html",
                data : { pageTitle: 'About' }
    });
    }
})(window, window.angular);
