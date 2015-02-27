(function (window, angular, undefined) {
    'use strict';

    angular.module('admin.stream').factory('streamData', streamData);

    streamData.$inject = ['$http'];

    function streamData($http) {
        var service = {
            getStream: getStream
        };

        return service;

        function getStream(tenant, streamId) {
            return $http.get(tenant + '/stream/' + streamId).then(function (d) {
                return d.data;
            });
        }
    }

})(window, window.angular);
