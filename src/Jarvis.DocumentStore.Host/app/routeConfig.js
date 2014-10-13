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
                templateUrl: "dashboard/dashboard.html"
            })
            .state('info', {
                url: "/info",
                templateUrl: "info/info.html"
            });
    }
})(window, window.angular);
