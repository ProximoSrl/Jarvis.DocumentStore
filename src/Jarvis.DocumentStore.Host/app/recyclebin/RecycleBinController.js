(function (window, angular, undefined) {
    'use strict';

    angular.module('admin.recyclebin').controller('RecycleBinController', RecycleBinController);

    RecycleBinController.$inject = ['$scope', 'recyclebinData'];

    function RecycleBinController($scope, recyclebinData) {
        var vm = this;
        vm.tenantId = "docs";

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

        function load() {
            recyclebinData.getDocuments(vm.tenantId, '', 1).then(function (data) {
                console.log('recyclebin documents from server', data);
                vm.documents = data.documents;
                vm.totalDocuments = data.count;
            });
        };

        function pageChanged(newPage) {
            vm.page = newPage;
            refresh();
        };

        vm.load();
    }
})(window, window.angular);
