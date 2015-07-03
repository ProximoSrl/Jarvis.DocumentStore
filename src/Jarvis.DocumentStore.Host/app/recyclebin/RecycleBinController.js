(function (window, angular, undefined) {
    'use strict';

    angular.module('admin.recyclebin').controller('RecycleBinController', RecycleBinController);

    RecycleBinController.$inject = ['$scope', 'recyclebinData', 'configService'];

    function RecycleBinController($scope, recyclebinData, configService) {
        var vm = this;
        vm.tenantId = "";
        vm.tenants = [];

        vm.documents = [];
        vm.totalDocuments = 0;
        vm.filters = {
            info: false,
            warn: false,
            error: false,
            debug: false,
            searchText: ''
        };

        vm.load = load;
        vm.page = 1;
        vm.pageChanged = pageChanged;

        configService.getTenants().then(function (d) {
            console.log('Get Tenants', d);
            vm.tenants = d;
            vm.tenantId = d[0];
            load();
        });

        function load() {
            recyclebinData.getDocuments(vm.tenantId, '', vm.page).then(function (data) {
                console.log('recyclebin documents from server', data);
                vm.documents = data.documents;
                vm.totalDocuments = data.count;
            });
        };

        function pageChanged(newPage) {
            vm.page = newPage;
            load();
        };

    }
})(window, window.angular);
