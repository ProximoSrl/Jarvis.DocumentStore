(function (window, angular, undefined) {
    'use strict';

    angular.module('admin.logs').factory('logsData', logsData);

    logsData.$inject = ['$http'];

    function logsData($http) {
        var service = {
            getLogs: getLogs
        };

        return service;

        function getLogs() {
            return $http.post('diagnostic/log').then(function (d) {
                return d.data;
            });
        }
    }

})(window, window.angular);
