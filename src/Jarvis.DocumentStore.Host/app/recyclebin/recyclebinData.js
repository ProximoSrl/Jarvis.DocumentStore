(function (window, angular, undefined) {
    'use strict';

    angular.module('admin.recyclebin').factory('recyclebinData', recyclebinData);

    recyclebinData.$inject = ['$http'];

    function recyclebinData($http) {
        var service = {
            getDocuments: getDocuments
        };

        return service;

        function getDocuments(tenant, filters, page) {
          
            var requestUrl = tenant + "/recyclebin/documents";

            var request = {
                Page: page,
                PageSize: 20,
                Filter: ''
            };

            return $http.post(requestUrl, request).then(function (d) {
                return d.data;
            });
        }
    }

})(window, window.angular);
