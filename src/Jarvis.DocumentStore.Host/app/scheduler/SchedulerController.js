(function (window, angular, undefined) {
    'use strict';

    angular.module('admin.scheduler').controller('SchedulerController', SchedulerController);

    SchedulerController.$inject = ['$interval', '$scope', 'schedulerData'];

    function SchedulerController($interval, $scope, schedulerData) {
        var vm = this;

        vm.status = 'off';
        vm.start = startScheduler;
        vm.stop = stopScheduler;
        vm.restartJob = restartJob;
        vm.info = null;

        /* */
        var stop = $interval(function () {
            schedulerData.isRunning().then(function (running) {
                vm.status = running ? "on" : "off";
            });
            schedulerData.getJobsInfo().then(function (data) {
                vm.info = data.data;
            });
        }, 1000);


        function startScheduler() {
            schedulerData.start();
        }

        function stopScheduler() {
            schedulerData.stop();
        }

        function restartJob(queueId)
        {
            schedulerData.restartJob(queueId);
        }

        $scope.$on('$destroy', function () {
            $interval.cancel(stop);
        });
    }
})(window, window.angular);
