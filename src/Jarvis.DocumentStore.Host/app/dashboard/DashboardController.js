(function (window, angular, undefined) {
    'use strict';

    angular.module('admin.dashboard').controller('DashboardController', DashboardController);

    DashboardController.$inject = ['dashboardData', '$interval', '$scope'];

    function DashboardController(dashboardData, $interval, $scope) {
        var vm = this;
        vm.title = 'prova';
        vm.meters = {
            "documents": 0,
            "docBytes": 1230,
            "handles": 0,
            "files": 0,
            "jobs": 0
        };
        vm.triggers = [];

        var update = function() {
            dashboardData.getMeters().then(function (d) {
                console.log('meters', d);
                vm.meters = d.docs;
                vm.triggers = d.triggers;
            });
        };

        update();
        var stop = $interval(update, 10000);

        $scope.$on('$destroy', function () {
            $interval.cancel(stop);
        });
    }
})(window, window.angular);
