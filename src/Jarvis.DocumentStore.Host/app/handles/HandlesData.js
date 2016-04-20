(function (window, angular, undefined) {
    'use strict';

    angular.module('admin.handles').factory('handlesData', logsData);

    logsData.$inject = ['$http'];

    function logsData($http) {
        var service = {
            get: get,
            reQueue : reQueue,
        };

        return service;

        function get(tenantId, handle) {
            
            return $http.get(tenantId + '/documents/info/' + handle).then(function (d) {
             
                return d.data;
            });
        }

        function reQueue(tenantId, documentHandle)
        {
            return $http.get("/queue/requeue/" + tenantId + "/" + documentHandle)
                .then(function (d) { return d.data });
        }
    }

})(window, window.angular);
