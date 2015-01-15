(function (window, angular, undefined) {
    'use strict';

    angular.module('admin.dashboard').factory('dashboardData', dashboardData);

    dashboardData.$inject= ['$http'];

    function dashboardData($http) {
        var service = {
            getMeters: getMeters
        };

        return service;

        function getMeters(tenant) {
            return $http.get('/'+tenant+'/dashboard').then(function(d) {
                return d.data;
            });
        }
    }

})(window, window.angular);
