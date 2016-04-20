(function (window, angular, undefined) {
    'use strict';

    angular.module('admin.handles').controller('HandlesController', HandlesController);

    HandlesController.$inject = ['$scope', 'handlesData', 'configService'];

    function HandlesController($scope, handlesData, configService) {
        var vm = this;

        vm.searchText = '';

        vm.search = function () {
            console.log("SEARCH: " + vm.searchText);
            handlesData.get(vm.tenantId, vm.searchText)
                .then(function (data) {
                    vm.data = data;
                });
        };

        vm.reQueue = function (documentHandle)
        {
            console.log("RE-QUEUE:", vm.tenantId, documentHandle);
            handlesData.reQueue(vm.tenantId, documentHandle);
        }

        configService.getTenants().then(function (d) {
            console.log('Get Tenants', d);
            vm.tenants = d;
            vm.tenantId = d[0];
        });
        
    }
})(window, window.angular);
