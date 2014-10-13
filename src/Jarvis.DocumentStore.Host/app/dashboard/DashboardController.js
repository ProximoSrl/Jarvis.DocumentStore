(function (window, angular, undefined) {
    'use strict';

    angular.module('admin.dashboard').controller('Dashboard', Dashboard);

    Dashboard.$inject = ['dashboardData'];

    function Dashboard(dashboardData) {
        var vm = this;

        vm.meters = {
            "documents": 0,
            "docBytes": 0,
            "handles": 0,
            "files": 0,
            "jobs": 0
        };


        dashboardData.getMeters().then(function(d) {
            vm.meters = d;
        });
    }
})(window, window.angular);
