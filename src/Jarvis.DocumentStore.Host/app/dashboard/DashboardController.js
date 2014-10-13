(function (window, angular, undefined) {
    'use strict';

    angular.module('admin.dashboard').controller('Dashboard', Dashboard);

    Dashboard.$inject = ['dashboardData'];

    function Dashboard(dashboardData) {
        var vm = this;

        vm.meters = {};

        dashboardData.getMeters().then(function(d) {
            vm.meters = d;
        });
    }
})(window, window.angular);
