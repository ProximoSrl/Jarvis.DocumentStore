(function (window, angular, undefined) {
    'use strict';

    angular.module('admin.logs').controller('LogsController', LogsController);

    LogsController.$inject = ['logsData'];

    function LogsController(logsData) {
        var vm = this;

        vm.items = [];

        logsData.getLogs().then(function(data) {
            console.log('logs from server', data);
            vm.items = data;
        });
    }
})(window, window.angular);
