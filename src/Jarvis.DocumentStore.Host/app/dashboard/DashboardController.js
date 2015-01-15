(function (window, angular, undefined) {
    'use strict';

    angular.module('admin.dashboard').controller('DashboardController', DashboardController);

    DashboardController.$inject = ['dashboardData', '$interval', '$scope', 'tenants','schedulerData'];

    function DashboardController(dashboardData, $interval, $scope, tenants, schedulerData) {
        console.log('tenants are ', tenants);
        console.log('sched', schedulerData);

        var vm = this;
        vm.title = 'prova';
        vm.stats = {};
        vm.tenants = tenants;
        vm.triggers = [];
        vm.totTriggers = 0;

        /* */

        var init = function () {
            angular.forEach(tenants, function (tname) {
                vm.stats[tname] ={
                    "tenant": tname,
                    "documents": 0,
                    "docBytes": 0,
                    "handles": 0,
                    "files": 0
                };
            });

            update();
        }

        var update = function () {
            angular.forEach(tenants, function (tname) {
                console.log('getting data for tenant ', tname);
                dashboardData.getMeters(tname).then(function (d) {
                    vm.stats[tname] = d;
                });
            });

            schedulerData.getStats().then(function (d) {
                vm.triggers = d.data;
                vm.totTriggers = 0;
                angular.forEach(vm.triggers, function (t) {
                    vm.totTriggers += t.count;
                });
            });
        };

        $scope.$on('$destroy', function () {
            $interval.cancel(stop);
        });

        init();

        var stop = $interval(update, 5000);
    }
})(window, window.angular);
