(function (window, angular, undefined) {
    'use strict';

    angular.module('admin.dashboard').controller('Dashboard', Dashboard);

    Dashboard.$inject = ['dashboardData','$interval','$scope'];

    function Dashboard(dashboardData, $interval,$scope) {
        var vm = this;

        vm.meters = {
            "documents": 0,
            "docBytes": 0,
            "handles": 0,
            "files": 0,
            "jobs": 0
        };

        /* */
        var stop = $interval(function() {
            dashboardData.getMeters().then(function(d) {
                vm.meters = d;
            });
        },1000);

        $scope.$on('$destroy', function () {
            $interval.cancel(stop);
        });
    }
})(window, window.angular);
