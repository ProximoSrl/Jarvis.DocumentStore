(function (window, angular, undefined) {
    'use strict';

    angular.module('admin.logs').controller('LogsController', LogsController);

    LogsController.$inject = ['$scope', 'logsData'];

    function LogsController($scope, logsData) {
        var vm = this;

        vm.items = [];
        vm.filters = {
            info: false,
            warn: false,
            error: false,
            debug: false
        };

        vm.refresh = refresh;

        start();

        /* */
        function start() {
            refresh();
        };

        function refresh() {
            logsData.getLogs(vm.filters).then(function (data) {
                console.log('logs from server', data);
                vm.items = data;
            });
        };

        $scope.$watch(function() {
            return vm.filters.info + '|'
                + vm.filters.debug + '|'
                + vm.filters.warn + '|'
                + vm.filters.error;
        }, function() {
            refresh();
        });
    }
})(window, window.angular);
