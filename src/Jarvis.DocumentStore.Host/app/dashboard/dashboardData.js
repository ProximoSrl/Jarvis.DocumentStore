(function (window, angular, undefined) {
    'use strict';

    angular.module('admin.dashboard').factory('dashboardData', dashboardData);

    dashboardData.$inject= ['$http'];

    function dashboardData($http) {
        debugger;
        var service = {
            getMeters: getMeters
        };

        return service;

        function getMeters() {
            return $http.get('/dashboard').then(function(d) {
                return d.data;
            });
        }
    }

})(window, window.angular);
